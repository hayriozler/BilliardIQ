using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Scoreboard.Client.Services;

public partial class RemoteSyncService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<RemoteSyncOptions> options,
    SystemPowerService systemPower,
    ILogger<RemoteSyncService> logger) : BackgroundService
{
    private const int _scoreDistributionBucketMinutes = 5;

    private const int _maxUnsentMatches = 200;

    private readonly RemoteSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
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
                catch (Exception ex) when (ex is not OperationCanceledException)
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

            var scoreDistribution = await db.MatchScoreStatSet
                .Where(s => s.MatchResultId == match.Id)
                .OrderBy(s => s.PlayerSlot).ThenBy(s => s.BucketIndex)
                .Select(s => new { playerSlot = s.PlayerSlot, bucketIndex = s.BucketIndex, totalPoints = s.TotalPoints })
                .ToListAsync(ct);

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
                winner = match.Winner,
                playedAt = match.PlayedAt,
                startedAt = match.StartedAt,
                endedAt = match.EndedAt,
                scoreDistributionBucketMinutes = _scoreDistributionBucketMinutes,
                scoreDistribution
            };

            LogSendMatchResult(match.Id, match.Player1Name, match.Player1Score, match.Player2Name, match.Player2Score, match.Winner);
            var response = await http.PostAsJsonAsync("stats", payload, ct);
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

        if (doomedIds.Count == 0)
        {
            return;
        }

        await db.MatchScoreStatSet.Where(s => doomedIds.Contains(s.MatchResultId)).ExecuteDeleteAsync(ct);
        await db.MatchResultSet.Where(m => doomedIds.Contains(m.Id)).ExecuteDeleteAsync(ct);
    }

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

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED response for match {MatchId}: {Status} {Body}")]
    private partial void LogReceivedMatchResultResponse(int matchId, System.Net.HttpStatusCode status, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push match result {MatchId} failed: {Status}")]
    private partial void LogPushMatchResultFailed(int matchId, System.Net.HttpStatusCode status);
}
