using System.Net.WebSockets;
using System.Text;
using Zeymera.BillardIQ.Client.Models;
using Zeymera.BillardIQ.Client.Services;

namespace Zeymera.BillardIQ.Client.Endpoints;

public static partial class ScoreboardWebSocketEndpoint
{
    private const int MaxControlMessageBytes = 16 * 1024 * 1024;

    public static void MapScoreboardWebSocket(this IEndpointRouteBuilder app)
    {
        app.Map("/ws", async (HttpContext context, ScoreboardCommandHub hub, ILogger<Program> logger) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            LogWsConnected(logger, remoteIp);
            hub.ControllerConnected(socket, remoteIp);
            try
            {
                var buffer = new byte[8192];
                while (socket.State == WebSocketState.Open)
                {
                    using var messageStream = new MemoryStream();
                    WebSocketReceiveResult result;
                    var closed = false;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                            closed = true;
                            break;
                        }

                        messageStream.Write(buffer, 0, result.Count);
                        if (messageStream.Length > MaxControlMessageBytes)
                        {
                            LogMessageTooLarge(logger, remoteIp, MaxControlMessageBytes);
                            await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too large", CancellationToken.None);
                            closed = true;
                            break;
                        }
                    } while (!result.EndOfMessage);

                    if (closed)
                    {
                        LogWsClosedBy(logger, remoteIp);
                        break;
                    }

                    var seq = hub.NextSequence();
                    var text = Encoding.UTF8.GetString(messageStream.ToArray());
                    LogRecv(logger, seq, remoteIp, text);

                    var messages = ScoreboardCommandParser.TryParse(text);
                    if (messages.Count > 0)
                    {
                        if (logger.IsEnabled(LogLevel.Information))
                        {
                            LogParsedCommands(logger, seq, messages.Count, remoteIp, string.Join(", ", messages.Select(m => m.Command)));
                        }

                        hub.Publish(socket, messages);
                    }
                    else
                    {
                        LogNoCommandParsed(logger, seq, remoteIp, text);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogWsCancelled(logger, remoteIp);
            }
            catch (WebSocketException ex)
            {
                LogWsEndedAbruptly(logger, ex, remoteIp);
            }
            finally
            {
                hub.ControllerDisconnected(socket);
                LogWsDisconnected(logger, remoteIp);
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "WS connected: {RemoteIp}")]
    private static partial void LogWsConnected(ILogger logger, string remoteIp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message from {RemoteIp} exceeded {MaxBytes} bytes - closing connection")]
    private static partial void LogMessageTooLarge(ILogger logger, string remoteIp, int maxBytes);

    [LoggerMessage(Level = LogLevel.Information, Message = "WS closed by {RemoteIp}")]
    private static partial void LogWsClosedBy(ILogger logger, string remoteIp);

    [LoggerMessage(Level = LogLevel.Information, Message = "[#{Seq}] RECV from {RemoteIp}: {Text}")]
    private static partial void LogRecv(ILogger logger, long seq, string remoteIp, string text);

    [LoggerMessage(Level = LogLevel.Information, Message = "[#{Seq}] Parsed {Count} command(s) from {RemoteIp}: {Commands}")]
    private static partial void LogParsedCommands(ILogger logger, long seq, int count, string remoteIp, string commands);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[#{Seq}] Could not parse any command from {RemoteIp}: {Text}")]
    private static partial void LogNoCommandParsed(ILogger logger, long seq, string remoteIp, string text);

    [LoggerMessage(Level = LogLevel.Information, Message = "WS connection from {RemoteIp} cancelled (dropped without a close handshake)")]
    private static partial void LogWsCancelled(ILogger logger, string remoteIp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "WS connection from {RemoteIp} ended abruptly")]
    private static partial void LogWsEndedAbruptly(ILogger logger, Exception ex, string remoteIp);

    [LoggerMessage(Level = LogLevel.Information, Message = "WS disconnected: {RemoteIp}")]
    private static partial void LogWsDisconnected(ILogger logger, string remoteIp);
}
