using BilliardIQ.Mobile.Models;

namespace BilliardIQ.Mobile.Services;

public sealed class RaspberryPiConnectionService(IPiTransportFactory transportFactory) : IRaspberryPiConnectionService, IDisposable
{
    private IPiTransport? _activeTransport;
    private ConnectionType? _lastConnectionType;
    private string? _lastWebSocketUri;
    public PiConnectionState State { get; private set; } = PiConnectionState.Disconnected;
    public ConnectionType? ActiveConnectionType { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public event EventHandler<PiConnectionState>? StateChanged;
    public event EventHandler<string>? MessageReceived;

    public Task<bool> ConnectViaWebSocketAsync(string uri, CancellationToken cancellationToken = default) =>
        ConnectAsync(ConnectionType.WebSocket, transportFactory.CreateWebSocket(uri), () =>
        {
            _lastConnectionType = ConnectionType.WebSocket;
            _lastWebSocketUri = uri;
        }, cancellationToken);

    private async Task<bool> ConnectAsync(ConnectionType type, IPiTransport transport, Action rememberForReconnect, CancellationToken cancellationToken)
    {
        await DisconnectAsync();
        SetState(PiConnectionState.Connecting);

        transport.MessageReceived += OnTransportMessageReceived;
        transport.Faulted += OnTransportFaulted;
        try
        {
            await transport.ConnectAsync(cancellationToken);

            _activeTransport = transport;
            ActiveConnectionType = type;
            rememberForReconnect();
            SetState(PiConnectionState.Connected);
            return true;
        }
        catch (OperationCanceledException)
        {
            transport.MessageReceived -= OnTransportMessageReceived;
            transport.Faulted -= OnTransportFaulted;
            transport.Dispose();
            SetState(PiConnectionState.Disconnected);
            return false;
        }
        catch (Exception ex)
        {
            transport.MessageReceived -= OnTransportMessageReceived;
            transport.Faulted -= OnTransportFaulted;
            transport.Dispose();
            LastErrorMessage = ex.Message;
            SetState(PiConnectionState.Failed);
            return false;
        }
    }

    public Task<bool> ReconnectAsync(CancellationToken cancellationToken = default) => _lastConnectionType switch
    {
        ConnectionType.WebSocket when _lastWebSocketUri is not null => ConnectViaWebSocketAsync(_lastWebSocketUri, cancellationToken),
        _ => Task.FromResult(false)
    };

    public async Task DisconnectAsync()
    {
        if (_activeTransport is { } transport)
        {
            transport.MessageReceived -= OnTransportMessageReceived;
            transport.Faulted -= OnTransportFaulted;
            await transport.DisconnectAsync();
            transport.Dispose();
            _activeTransport = null;
        }

        ActiveConnectionType = null;
        SetState(PiConnectionState.Disconnected);
    }

    public Task SendMessageAsync(string message)
    {
        if (State != PiConnectionState.Connected || _activeTransport is null)
            throw new InvalidOperationException("Cannot send: not connected to the scoreboard.");

        return _activeTransport.SendMessageAsync(message);
    }

    private void OnTransportMessageReceived(object? sender, string message) => MessageReceived?.Invoke(this, message);

    private void OnTransportFaulted(object? sender, Exception ex)
    {
        LastErrorMessage = ex.Message;
        SetState(PiConnectionState.Failed);
    }

    private void SetState(PiConnectionState state)
    {
        State = state;
        StateChanged?.Invoke(this, state);
    }

    public void Dispose() => _activeTransport?.Dispose();
}
