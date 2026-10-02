using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Scoreboard.Client.Services;

public class BoardSessionGuard
{
    private readonly Lock _lock = new();
    private Guid? _activeSessionId;
    private bool _connected;

    public bool TryAcquire(out Guid sessionId)
    {
        lock (_lock)
        {
            if (_activeSessionId is not null && _connected)
            {
                sessionId = Guid.Empty;
                return false;
            }

            sessionId = Guid.NewGuid();
            _activeSessionId = sessionId;
            _connected = true;
            return true;
        }
    }

    public event Action<Guid>? SessionReplaced;

    public Guid ForceAcquire()
    {
        Guid? replaced;
        Guid sessionId;
        lock (_lock)
        {
            replaced = _activeSessionId;
            sessionId = Guid.NewGuid();
            _activeSessionId = sessionId;
            _connected = true;
        }

        if (replaced is Guid previous)
        {
            SessionReplaced?.Invoke(previous);
        }

        return sessionId;
    }

    public void SetConnected(Guid sessionId, bool connected)
    {
        lock (_lock)
        {
            if (_activeSessionId == sessionId)
            {
                _connected = connected;
            }
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

public class BoardSessionTracker
{
    public Guid? SessionId { get; set; }
}

public class BoardCircuitHandler(BoardSessionTracker tracker, BoardSessionGuard guard) : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (tracker.SessionId is Guid id)
        {
            guard.SetConnected(id, false);
        }

        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (tracker.SessionId is Guid id)
        {
            guard.SetConnected(id, true);
        }

        return Task.CompletedTask;
    }
}
