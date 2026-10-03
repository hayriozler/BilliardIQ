namespace Scoreboard.Client.Services;

public sealed class TransientRetryHandler(ILogger<TransientRetryHandler> logger) : DelegatingHandler
{
    private static readonly TimeSpan[] _delays =
    [
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(15)
    ];

    private static readonly TimeSpan _attemptTimeout = TimeSpan.FromSeconds(30);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var idempotent = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;
        for (var attempt = 0; ; attempt++)
        {
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptCts.CancelAfter(_attemptTimeout);
            string reason;
            try
            {
                var response = await base.SendAsync(request, attemptCts.Token);
                if (!idempotent || attempt >= _delays.Length || response.StatusCode is not (System.Net.HttpStatusCode.BadGateway or System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.GatewayTimeout))
                {
                    return response;
                }

                reason = $"HTTP {(int)response.StatusCode}";
                response.Dispose();
            }
            catch (HttpRequestException ex) when (attempt < _delays.Length && !cancellationToken.IsCancellationRequested
                && (idempotent || ex.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError))
            {
                reason = ex.HttpRequestError.ToString();
            }
            catch (OperationCanceledException) when (idempotent && attempt < _delays.Length && !cancellationToken.IsCancellationRequested)
            {
                reason = "timeout";
            }

            var delay = _delays[attempt];
            logger.LogWarning("{Method} {Uri} failed ({Reason}), retry {Attempt} of {Max} in {Delay} s", request.Method, request.RequestUri, reason, attempt + 1, _delays.Length, delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);
        }
    }
}
