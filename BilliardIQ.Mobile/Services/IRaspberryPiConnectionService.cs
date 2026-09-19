using BilliardIQ.Mobile.Models;

namespace BilliardIQ.Mobile.Services;

public interface IRaspberryPiConnectionService
{
    PiConnectionState State { get; }
    ConnectionType? ActiveConnectionType { get; }
    string? LastErrorMessage { get; }

    event EventHandler<PiConnectionState>? StateChanged;
    event EventHandler<string>? MessageReceived;

    Task<IReadOnlyList<BluetoothDeviceInfo>> ScanForBluetoothDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<bool> ConnectViaWebSocketAsync(string uri, CancellationToken cancellationToken = default);
    Task<bool> ConnectViaBluetoothAsync(BluetoothDeviceInfo device, CancellationToken cancellationToken = default);

    // Retries whichever transport last succeeded, using the same URI/device.
    // Returns false if we've never connected this session (nothing to retry).
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync();
    Task SendMessageAsync(string message);
}
