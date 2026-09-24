using BilliardIQ.Mobile.Models;

namespace BilliardIQ.Mobile.Services;

public interface IRaspberryPiConnectionService
{
    PiConnectionState State { get; }
    ConnectionType? ActiveConnectionType { get; }
    string? LastErrorMessage { get; }

    event EventHandler<PiConnectionState>? StateChanged;
    event EventHandler<string>? MessageReceived;

    Task<bool> ConnectViaWebSocketAsync(string uri, CancellationToken cancellationToken = default);
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();
    Task SendMessageAsync(string message);
}
