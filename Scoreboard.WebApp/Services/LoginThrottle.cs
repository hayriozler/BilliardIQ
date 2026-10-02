using System.Collections.Concurrent;

namespace Scoreboard.WebApp.Services;

public sealed class LoginThrottle
{
    private const int MaxFailures = 5;
    private const int CleanupThreshold = 5000;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new();

    private sealed record Attempts(int Count, DateTimeOffset LastFailureAt);

    public bool IsLocked(string key) =>
        _attempts.TryGetValue(key, out var attempts)
        && attempts.Count >= MaxFailures
        && DateTimeOffset.UtcNow - attempts.LastFailureAt < LockDuration;

    public void RecordFailure(string key)
    {
        var now = DateTimeOffset.UtcNow;
        _attempts.AddOrUpdate(
            key,
            _ => new Attempts(1, now),
            (_, attempts) => now - attempts.LastFailureAt >= LockDuration ? new Attempts(1, now) : new Attempts(attempts.Count + 1, now));

        if (_attempts.Count > CleanupThreshold)
        {
            foreach (var (existing, attempts) in _attempts)
            {
                if (now - attempts.LastFailureAt >= LockDuration)
                {
                    _attempts.TryRemove(existing, out _);
                }
            }
        }
    }

    public void Reset(string key) => _attempts.TryRemove(key, out _);
}
