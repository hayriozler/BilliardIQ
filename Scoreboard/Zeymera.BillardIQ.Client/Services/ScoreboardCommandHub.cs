using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using Zeymera.BillardIQ.Client.Models;

namespace Zeymera.BillardIQ.Client.Services;

public partial class ScoreboardCommandHub(ILogger<ScoreboardCommandHub> logger)
{
    private sealed record Connection(SemaphoreSlim Gate, string RemoteIp);

    private readonly ConcurrentDictionary<WebSocket, Connection> _sockets = new();
    private long _sequence;

    public event Action<WebSocket, IReadOnlyList<ScoreboardCommandMessage>>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool HasActiveConnection => !_sockets.IsEmpty;

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    public void Publish(WebSocket sender, IReadOnlyList<ScoreboardCommandMessage> messages)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogPublishing(messages.Count, string.Join(", ", messages.Select(m => m.Command)));
        }

        CommandReceived?.Invoke(sender, messages);
    }

    public void ControllerConnected(WebSocket socket, string remoteIp)
    {
        _sockets[socket] = new Connection(new SemaphoreSlim(1, 1), remoteIp);
        LogClientRegistered(remoteIp, _sockets.Count);
        ConnectionChanged?.Invoke();
    }

    public void ControllerDisconnected(WebSocket socket)
    {
        if (_sockets.TryRemove(socket, out var connection))
        {
            connection.Gate.Dispose();
            LogClientUnregistered(connection.RemoteIp, _sockets.Count);
        }

        ConnectionChanged?.Invoke();
    }

    private static readonly TimeSpan _sendTimeout = TimeSpan.FromSeconds(5);

    public async Task SendAsync(WebSocket socket, string json, CancellationToken cancellationToken = default)
    {
        if (!_sockets.TryGetValue(socket, out var connection))
        {
            LogSendToUnregisteredSocket();
            return;
        }

        var seq = NextSequence();
        await connection.Gate.WaitAsync(cancellationToken);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                using var timeoutCts = new CancellationTokenSource(_sendTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, linkedCts.Token);
                LogSent(seq, connection.RemoteIp, json);
            }
            else
            {
                LogSendSkippedSocketState(seq, connection.RemoteIp, socket.State);
            }
        }
        catch (WebSocketException ex)
        {
            LogSendFailedWebSocketException(ex, seq, connection.RemoteIp);
        }
        catch (ObjectDisposedException)
        {
            LogSendFailedSocketDisposed(seq, connection.RemoteIp);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogSendTimedOut(seq, connection.RemoteIp, _sendTimeout);
        }
        finally
        {
            connection.Gate.Release();
        }
    }

    public async Task BroadcastStateAsync(ScoreboardStateSnapshot snapshot, WebSocket? excludeSocket = null)
    {
        var targets = _sockets.Keys.Where(socket => socket != excludeSocket).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var json = ScoreboardStateSnapshotFactory.ToWireJson(snapshot);
        if (logger.IsEnabled(LogLevel.Information))
        {
            LogBroadcasting(targets.Count, string.Join(", ", targets.Select(socket => _sockets[socket].RemoteIp)));
        }

        await Task.WhenAll(targets.Select(socket => SendAsync(socket, json)));
    }

    public async Task BroadcastJsonAsync(string json)
    {
        var targets = _sockets.Keys.ToList();
        if (targets.Count == 0)
        {
            return;
        }

        await Task.WhenAll(targets.Select(socket => SendAsync(socket, json)));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Publishing {Count} command(s) to the board: {Commands}")]
    private partial void LogPublishing(int count, string commands);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hub: registered client {RemoteIp} ({Count} total)")]
    private partial void LogClientRegistered(string remoteIp, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hub: unregistered client {RemoteIp} ({Count} remaining)")]
    private partial void LogClientUnregistered(string remoteIp, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Hub: SendAsync called for an unregistered socket - message dropped")]
    private partial void LogSendToUnregisteredSocket();

    [LoggerMessage(Level = LogLevel.Information, Message = "[#{Seq}] SENT to {RemoteIp}: {Json}")]
    private partial void LogSent(long seq, string remoteIp, string json);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[#{Seq}] Skipped send to {RemoteIp} - socket state is {State}")]
    private partial void LogSendSkippedSocketState(long seq, string remoteIp, WebSocketState state);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[#{Seq}] Send to {RemoteIp} failed (WebSocketException)")]
    private partial void LogSendFailedWebSocketException(Exception ex, long seq, string remoteIp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[#{Seq}] Send to {RemoteIp} failed - socket already disposed")]
    private partial void LogSendFailedSocketDisposed(long seq, string remoteIp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[#{Seq}] Send to {RemoteIp} timed out after {Timeout}")]
    private partial void LogSendTimedOut(long seq, string remoteIp, TimeSpan timeout);

    [LoggerMessage(Level = LogLevel.Information, Message = "Broadcasting state to {Count} client(s): {RemoteIps}")]
    private partial void LogBroadcasting(int count, string remoteIps);
}
