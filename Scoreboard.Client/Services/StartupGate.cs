namespace Scoreboard.Client.Services;

public sealed class StartupGate
{
    private static readonly TimeSpan _settle = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan _fallback = TimeSpan.FromSeconds(180);

    private readonly TaskCompletionSource _rendered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkBoardRendered() => _rendered.TrySetResult();

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        using var fallbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var fallback = Task.Delay(_fallback, fallbackCts.Token);
        await Task.WhenAny(_rendered.Task, fallback);
        await fallbackCts.CancelAsync();
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(_settle, cancellationToken);
    }
}
