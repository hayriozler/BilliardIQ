namespace Scoreboard.Client.Services;

public sealed class ServerClock
{
    private static readonly TimeSpan _tolerance = TimeSpan.FromSeconds(15);

    private long _offsetTicks;

    private TimeZoneInfo _zone = TimeZoneInfo.Local;

    public DateTime Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow.AddTicks(Interlocked.Read(ref _offsetTicks)), Volatile.Read(ref _zone)).DateTime;

    public void Observe(DateTimeOffset serverTime)
    {
        var difference = serverTime - DateTimeOffset.UtcNow;
        Interlocked.Exchange(ref _offsetTicks, difference.Duration() > _tolerance ? difference.Ticks : 0);
    }

    public void UseTimeZone(string? id)
    {
        var zone = TimeZoneInfo.Local;
        if (!string.IsNullOrWhiteSpace(id))
        {
            try
            {
                zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
            }
        }

        Volatile.Write(ref _zone, zone);
    }
}

public sealed class ServerClockHandler(ServerClock clock) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.Headers.Date is { } date)
        {
            clock.Observe(date);
        }

        return response;
    }
}
