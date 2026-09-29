namespace Scoreboard.Client.Services;

using System.Net.WebSockets;
using System.Text;

public class WebSocketService
{
    private ClientWebSocket? _socket;

    public event Action<string>? ValueReceived;

    public async Task ConnectAsync(string url)
    {
        _socket = new ClientWebSocket();

        await _socket.ConnectAsync(
            new Uri(url),
            CancellationToken.None);

        _ = ReceiveLoop();
    }

    private async Task ReceiveLoop()
    {
        if (_socket == null)
            return;

        var buffer = new byte[4096];

        while (_socket.State == WebSocketState.Open)
        {
            var result = await _socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var message = Encoding.UTF8.GetString(
                buffer,
                0,
                result.Count);

            ValueReceived?.Invoke(message);
        }
    }
}