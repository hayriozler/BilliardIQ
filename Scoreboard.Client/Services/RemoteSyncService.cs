using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Scoreboard.Client.Services;

public partial class RemoteSyncService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<RemoteSyncOptions> options,
    SystemPowerService systemPower,
    StartupGate startup,
    ILogger<RemoteSyncService> logger) : BackgroundService
{
    private const int _maxUnsentMatches = 200;

    private const int _maxHistoryRows = 1000;

    private const int _historyKeepDays = 3;

    private readonly RemoteSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await startup.WaitAsync(stoppingToken);

            HttpClient? http = null;
            if (!_options.Enabled)
            {
                LogSyncDisabled();
            }
            else if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                LogSyncNoBaseUrl();
            }
            else if (string.IsNullOrWhiteSpace(_options.ClientId) || _options.TableNo <= 0)
            {
                LogSyncNoIdentity();
            }
            else
            {
                http = httpClientFactory.CreateClient(nameof(RemoteSyncService));
                http.BaseAddress = new Uri(_options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/");
                http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);
                http.DefaultRequestHeaders.Add("X-Table-No", _options.TableNo.ToString());
            }

            var interval = TimeSpan.FromSeconds(Math.Max(5, http is null ? 300 : _options.PollSeconds));
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await SyncOnceAsync(http, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogSyncTickFailed(ex);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogSyncStartFailed(ex);
        }
    }

    private async Task SyncOnceAsync(HttpClient? http, CancellationToken ct)
    {
        if (systemPower.IsShuttingDown)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        if (http is not null)
        {
            await PushMatchResultsAsync(db, http, ct);
        }

        await CleanupMatchesAsync(db, ct);
    }

    private async Task PushMatchResultsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.MatchResultSet.Where(m => !m.SyncedAPI).OrderBy(m => m.Id).ToListAsync(ct);
        foreach (var match in pending)
        {
            var playerIds = new[] { match.Player1Id, match.Player2Id };
            var seedIds = await db.PlayerSet
                .Where(p => playerIds.Contains(p.Id) && p.IsSystem && p.UpdatedAt == null)
                .Select(p => p.Id)
                .ToListAsync(ct);
            int? ServerId(int id) => id > 0 && !seedIds.Contains(id) ? id : null;

            var historyRows = await db.MatchHistorySet
                .Where(h => h.MatchResultId == match.Id)
                .OrderBy(h => h.Id)
                .ToListAsync(ct);
            var history = historyRows
                .Select(h => new
                {
                    playerId = ServerId(h.PlayerId),
                    playerSlot = h.PlayerSlot,
                    inning = h.Inning,
                    score = h.Score,
                    totalScore = h.TotalScore,
                    playedAt = ToUtc(h.Timestamp)
                })
                .ToList();

            var payload = new
            {
                player1Id = ServerId(match.Player1Id),
                player1Name = match.Player1Name,
                player1Score = match.Player1Score,
                player1Avg = match.Player1Avg,
                player1HighRun = match.Player1HighRun,
                player2Id = ServerId(match.Player2Id),
                player2Name = match.Player2Name,
                player2Score = match.Player2Score,
                player2Avg = match.Player2Avg,
                player2HighRun = match.Player2HighRun,
                inning = match.Inning,
                matchTarget = match.MatchTarget,
                isHandicap = match.IsHandicap,
                player1Target = match.Player1Target,
                player2Target = match.Player2Target,
                winner = match.Winner,
                playedAt = ToUtc(match.PlayedAt),
                startedAt = ToUtc(match.StartedAt),
                endedAt = ToUtc(match.EndedAt),
                utcOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(match.EndedAt ?? match.PlayedAt).TotalMinutes,
                history
            };

            LogSendMatchResult(match.Id, match.Player1Name, match.Player1Score, match.Player2Name, match.Player2Score, match.Winner);
            var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
            LogSendMatchPayload(match.Id, json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("stats", content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            LogReceivedMatchResultResponse(match.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                LogPushMatchResultFailed(match.Id, response.StatusCode);
                continue;
            }

            match.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

    private static DateTime? ToUtc(DateTime? local) => local is { } value ? ToUtc(value) : null;

    private async Task CleanupMatchesAsync(DataContext db, CancellationToken ct)
    {
        var doomedIds = await db.MatchResultSet.Where(m => m.SyncedAPI).Select(m => m.Id).ToListAsync(ct);

        var unsentCount = await db.MatchResultSet.CountAsync(m => !m.SyncedAPI, ct);
        if (unsentCount > _maxUnsentMatches)
        {
            var excess = await db.MatchResultSet.Where(m => !m.SyncedAPI)
                .OrderBy(m => m.Id).Take(unsentCount - _maxUnsentMatches).Select(m => m.Id).ToListAsync(ct);
            LogDroppedUnsentMatches(excess.Count);
            doomedIds.AddRange(excess);
        }

        if (doomedIds.Count > 0)
        {
            await db.MatchHistorySet.Where(h => h.MatchResultId != null && doomedIds.Contains(h.MatchResultId.Value)).ExecuteDeleteAsync(ct);
            await db.MatchResultSet.Where(m => doomedIds.Contains(m.Id)).ExecuteDeleteAsync(ct);
        }

        await TrimHistoryAsync(db, ct);
    }

    private async Task TrimHistoryAsync(DataContext db, CancellationToken ct)
    {
        var total = await db.MatchHistorySet.CountAsync(ct);
        if (total <= _maxHistoryRows)
        {
            return;
        }

        var cutoff = DateTime.Now.AddDays(-_historyKeepDays);
        var removable = await db.MatchHistorySet
            .Where(h => h.Timestamp < cutoff)
            .OrderBy(h => h.Id)
            .Take(total - _maxHistoryRows)
            .Select(h => h.Id)
            .ToListAsync(ct);
        if (removable.Count == 0)
        {
            return;
        }

        var lastId = removable[^1];
        LogTrimmedHistory(removable.Count);
        await db.MatchHistorySet.Where(h => h.Timestamp < cutoff && h.Id <= lastId).ExecuteDeleteAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Trimmed {Count} old match history row(s): more than the local limit are stored.")]
    private partial void LogTrimmedHistory(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "RemoteSync is disabled (RemoteSync:Enabled=false) - match results are not sent; only the local backlog is kept bounded.")]
    private partial void LogSyncDisabled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemoteSync is enabled but RemoteSync:BaseUrl is empty - skipping match result push.")]
    private partial void LogSyncNoBaseUrl();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemoteSync is enabled but RemoteSync:ClientId or RemoteSync:TableNo is not set - skipping match result push.")]
    private partial void LogSyncNoIdentity();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote sync tick failed; will retry next interval.")]
    private partial void LogSyncTickFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "RemoteSync could not start; match result push is disabled for this run.")]
    private partial void LogSyncStartFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped {Count} oldest unsent match result(s): more than the local limit are waiting.")]
    private partial void LogDroppedUnsentMatches(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND match result {MatchId} to stats: {Player1Name}({Player1Score}) vs {Player2Name}({Player2Score}) winner={Winner}")]
    private partial void LogSendMatchResult(int matchId, string player1Name, int player1Score, string player2Name, int player2Score, int winner);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND payload for match {MatchId}: {Json}")]
    private partial void LogSendMatchPayload(int matchId, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED response for match {MatchId}: {Status} {Body}")]
    private partial void LogReceivedMatchResultResponse(int matchId, System.Net.HttpStatusCode status, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push match result {MatchId} failed: {Status}")]
    private partial void LogPushMatchResultFailed(int matchId, System.Net.HttpStatusCode status);
}
