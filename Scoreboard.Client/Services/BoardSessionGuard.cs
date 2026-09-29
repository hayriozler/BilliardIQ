namespace Scoreboard.Client.Services;

public class BoardSessionGuard
{
    private readonly Lock _lock = new();
    private Guid? _activeSessionId;

    public bool TryAcquire(out Guid sessionId)
    {
        lock (_lock)
        {
            if (_activeSessionId is not null)
            {
                sessionId = Guid.Empty;
                return false;
            }

            sessionId = Guid.NewGuid();
            _activeSessionId = sessionId;
            return true;
        }
    }

    public void Release(Guid sessionId)
    {
        lock (_lock)
        {
            if (_activeSessionId == sessionId)
            {
                _activeSessionId = null;
            }
        }
    }
}
