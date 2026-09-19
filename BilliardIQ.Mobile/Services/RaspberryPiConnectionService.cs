using BilliardIQ.Mobile.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Net.WebSockets;
using System.Text;

namespace BilliardIQ.Mobile.Services;

public sealed class RaspberryPiConnectionService : IRaspberryPiConnectionService, IDisposable
{
    private static readonly Guid _piServiceUuid = Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
    private static readonly Guid _piWriteCharacteristicUuid = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
    private static readonly Guid _piNotifyCharacteristicUuid = Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e");

    private readonly IBluetoothLE _bluetoothLe = CrossBluetoothLE.Current;

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _webSocketReceiveCts;
    private IDevice? _bluetoothDevice;
    private ICharacteristic? _writeCharacteristic;
    private ICharacteristic? _notifyCharacteristic;
    private EventHandler<CharacteristicUpdatedEventArgs>? _notifyHandler;

    private ConnectionType? _lastConnectionType;
    private string? _lastWebSocketUri;
    private BluetoothDeviceInfo? _lastBluetoothDevice;

    public PiConnectionState State { get; private set; } = PiConnectionState.Disconnected;
    public ConnectionType? ActiveConnectionType { get; private set; }
    public string? LastErrorMessage { get; private set; }

    public event EventHandler<PiConnectionState>? StateChanged;
    public event EventHandler<string>? MessageReceived;

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> ScanForBluetoothDevicesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var status = await Permissions.RequestAsync<BluetoothPermissions>();
        if (status != PermissionStatus.Granted)
        {
            LastErrorMessage = "Bluetooth permission was denied.";
            SetState(PiConnectionState.Failed);
            return [];
        }

        var adapter = _bluetoothLe.Adapter;
        var found = new List<BluetoothDeviceInfo>();

        foreach (var bonded in adapter.BondedDevices)
        {
            if (string.IsNullOrWhiteSpace(bonded.Name)) continue;
            var info = new BluetoothDeviceInfo(bonded.Id, bonded.Name);
            if (!found.Contains(info)) found.Add(info);
        }

        void OnDeviceDiscovered(object? sender, DeviceEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Device.Name)) return;
            var info = new BluetoothDeviceInfo(args.Device.Id, args.Device.Name);
            if (!found.Contains(info)) found.Add(info);
        }

        adapter.DeviceDiscovered += OnDeviceDiscovered;
        try
        {
            adapter.ScanTimeout = (int)timeout.TotalMilliseconds;
            await adapter.StartScanningForDevicesAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            adapter.DeviceDiscovered -= OnDeviceDiscovered;
        }

        return found;
    }

    public async Task<bool> ConnectViaWebSocketAsync(string uri, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
        SetState(PiConnectionState.Connecting);
        try
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(uri), cancellationToken);
            _webSocket = socket;
            ActiveConnectionType = ConnectionType.WebSocket;
            _lastConnectionType = ConnectionType.WebSocket;
            _lastWebSocketUri = uri;
            SetState(PiConnectionState.Connected);

            _webSocketReceiveCts = new CancellationTokenSource();
            _ = ReceiveWebSocketLoopAsync(socket, _webSocketReceiveCts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            SetState(PiConnectionState.Disconnected);
            return false;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            SetState(PiConnectionState.Failed);
            return false;
        }
    }

    private async Task ReceiveWebSocketLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var messageBuffer = new MemoryStream();
        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                messageBuffer.SetLength(0);
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                        return;
                    }
                    messageBuffer.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            SetState(PiConnectionState.Failed);
        }
    }

    public async Task<bool> ConnectViaBluetoothAsync(BluetoothDeviceInfo device, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
        SetState(PiConnectionState.Connecting);
        try
        {
            var status = await Permissions.RequestAsync<BluetoothPermissions>();
            if (status != PermissionStatus.Granted)
                throw new InvalidOperationException("Bluetooth permission was denied.");

            var adapter = _bluetoothLe.Adapter;
            var nativeDevice = await adapter.ConnectToKnownDeviceAsync(device.Id, cancellationToken: cancellationToken);

            var service = await nativeDevice.GetServiceAsync(_piServiceUuid, cancellationToken)??throw new InvalidOperationException($"Raspberry Pi BLE service {_piServiceUuid} not found on '{device.Name}'.");
            var writeCharacteristic = await service.GetCharacteristicAsync(_piWriteCharacteristicUuid, cancellationToken)??throw new InvalidOperationException($"Raspberry Pi write characteristic {_piWriteCharacteristicUuid} not found.");
            var notifyCharacteristic = await service.GetCharacteristicAsync(_piNotifyCharacteristicUuid, cancellationToken);
            if (notifyCharacteristic is not null)
            {
                _notifyHandler = (_, args) =>
                {
                    var message = Encoding.UTF8.GetString(args.Characteristic.Value ?? []);
                    MessageReceived?.Invoke(this, message);
                };
                notifyCharacteristic.ValueUpdated += _notifyHandler;
                await notifyCharacteristic.StartUpdatesAsync(cancellationToken);
            }

            _bluetoothDevice = nativeDevice;
            _writeCharacteristic = writeCharacteristic;
            _notifyCharacteristic = notifyCharacteristic;
            ActiveConnectionType = ConnectionType.Bluetooth;
            _lastConnectionType = ConnectionType.Bluetooth;
            _lastBluetoothDevice = device;
            SetState(PiConnectionState.Connected);
            return true;
        }
        catch (OperationCanceledException)
        {
            SetState(PiConnectionState.Disconnected);
            return false;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.Message;
            SetState(PiConnectionState.Failed);
            return false;
        }
    }

    public Task<bool> ReconnectAsync(CancellationToken cancellationToken = default) => _lastConnectionType switch
    {
        ConnectionType.WebSocket when _lastWebSocketUri is not null => ConnectViaWebSocketAsync(_lastWebSocketUri, cancellationToken),
        ConnectionType.Bluetooth when _lastBluetoothDevice is not null => ConnectViaBluetoothAsync(_lastBluetoothDevice, cancellationToken),
        _ => Task.FromResult(false)
    };

    public async Task DisconnectAsync()
    {
        _webSocketReceiveCts?.Cancel();
        _webSocketReceiveCts = null;

        if (_webSocket is not null)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }
            catch
            {
            }

            _webSocket.Dispose();
            _webSocket = null;
        }

        if (_bluetoothDevice is not null)
        {
            if (_notifyCharacteristic is not null && _notifyHandler is not null)
            {
                _notifyCharacteristic.ValueUpdated -= _notifyHandler;
                _notifyHandler = null;
            }

            try
            {
                await _bluetoothLe.Adapter.DisconnectDeviceAsync(_bluetoothDevice);
            }
            catch
            {
            }

            _bluetoothDevice.Dispose();
            _bluetoothDevice = null;
            _writeCharacteristic = null;
            _notifyCharacteristic = null;
        }

        ActiveConnectionType = null;
        SetState(PiConnectionState.Disconnected);
    }

    public async Task SendMessageAsync(string message)
    {
        if (State != PiConnectionState.Connected)
            throw new InvalidOperationException("Cannot send: not connected to the scoreboard.");

        switch (ActiveConnectionType)
        {
            case ConnectionType.WebSocket when _webSocket is { State: WebSocketState.Open }:
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                break;

            case ConnectionType.Bluetooth when _writeCharacteristic is not null:
                await _writeCharacteristic.WriteAsync(Encoding.UTF8.GetBytes(message));
                break;

            default:
                throw new InvalidOperationException(
                    $"Cannot send over {ActiveConnectionType}: the connection is not in a sendable state.");
        }
    }

    private void SetState(PiConnectionState state)
    {
        State = state;
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        _webSocketReceiveCts?.Cancel();
        _webSocket?.Dispose();
        _bluetoothDevice?.Dispose();
    }
}
