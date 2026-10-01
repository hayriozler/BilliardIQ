using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Scoreboard.Client.Services;

/// <summary>
/// The board can be open in one window at a time. A window that was closed or reloaded keeps its server-side session for
/// a while (Blazor holds on to a disconnected circuit so the browser can reconnect), so a session whose connection is
/// down no longer blocks a new window, and a window can take the board over explicitly.
/// </summary>
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

    /// <summary>Takes the board even when another window holds it.</summary>
    public Guid ForceAcquire()
    {
        lock (_lock)
        {
            var sessionId = Guid.NewGuid();
            _activeSessionId = sessionId;
            _connected = true;
            return sessionId;
        }
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

/// <summary>Per browser connection (circuit): which board session it holds.</summary>
public class BoardSessionTracker
{
    public Guid? SessionId { get; set; }
}

/// <summary>Tells the guard when the window holding the board loses or regains its connection.</summary>
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
