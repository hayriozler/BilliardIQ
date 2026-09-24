using System.Net.WebSockets;
using System.Text;

namespace BilliardIQ.Mobile.Services;

internal sealed class WebSocketPiTransport(string uri) : IPiTransport
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveCts;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<Exception>? Faulted;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(uri), cancellationToken);
        _socket = socket;

        _receiveCts = new CancellationTokenSource();
        _ = ReceiveLoopAsync(socket, _receiveCts.Token);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
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
            Faulted?.Invoke(this, ex);
        }
    }

    public async Task DisconnectAsync()
    {
        _receiveCts?.Cancel();
        _receiveCts = null;

        if (_socket is null) return;

        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
        }
        catch
        {
        }

        _socket.Dispose();
        _socket = null;
    }

    public Task SendMessageAsync(string message)
    {
        if (_socket is not { State: WebSocketState.Open } socket)
            throw new InvalidOperationException("WebSocket is not open.");

        var bytes = Encoding.UTF8.GetBytes(message);
        return socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _socket?.Dispose();
    }
}
