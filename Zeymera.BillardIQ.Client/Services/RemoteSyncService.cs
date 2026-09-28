using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Zeymera.BillardIQ.Client.Services;

public partial class RemoteSyncService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    IOptions<RemoteSyncOptions> options,
    SystemPowerService systemPower,
    ILogger<RemoteSyncService> logger) : BackgroundService
{
    private readonly RemoteSyncOptions _options = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_options.Enabled)
            {
                LogSyncDisabled();
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                LogSyncNoBaseUrl();
                return;
            }

            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            var http = httpClientFactory.CreateClient(nameof(RemoteSyncService));
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
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

    private async Task SyncOnceAsync(HttpClient http, CancellationToken ct)
    {
        if (systemPower.IsShuttingDown)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await PushPlayersAsync(db, http, ct);
        await PushTeamsAsync(db, http, ct);
        await PushStatsAsync(db, http, ct);
        await CleanupSyncedMatchesAsync(db, ct);
    }

    private async Task PushTeamsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.TeamSet.Where(t => !t.SyncedAPI).ToListAsync(ct);
        foreach (var team in pending)
        {
            var payload = new
            {
                id = team.Id,
                name = team.Name
            };

            LogSendTeam(team.Id, team.Name);
            var response = await http.PostAsJsonAsync("teams", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            LogReceivedTeamResponse(team.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                LogPushTeamFailed(team.Id, response.StatusCode);
                continue;
            }

            team.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushPlayersAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.PlayerSet.Where(p => !p.SyncedAPI).ToListAsync(ct);
        foreach (var player in pending)
        {
            string? photoBase64 = null;
            string? photoExtension = null;
            if (!string.IsNullOrEmpty(player.PhotoPath))
            {
                var fullPath = Path.Combine(env.WebRootPath, player.PhotoPath);
                if (File.Exists(fullPath))
                {
                    photoBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(fullPath, ct));
                    photoExtension = Path.GetExtension(fullPath).TrimStart('.');
                }
            }

            var payload = new
            {
                id = player.Id,
                nickname = player.Nickname,
                name = player.Name,
                avatarId = player.AvatarId,
                photoBase64,
                photoExtension
            };

            LogSendPlayer(player.Id, player.Nickname, player.Name, player.AvatarId, photoBase64?.Length ?? 0);
            var response = await http.PostAsJsonAsync("players", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            LogReceivedPlayerResponse(player.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                LogPushPlayerFailed(player.Id, response.StatusCode);
                continue;
            }

            player.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushStatsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.MatchResultSet.Where(m => !m.SyncedAPI).ToListAsync(ct);
        foreach (var match in pending)
        {
            var payload = new
            {
                player1Id = match.Player1Id,
                player1Name = match.Player1Name,
                player1Score = match.Player1Score,
                player1Avg = match.Player1Avg,
                player1HighRun = match.Player1HighRun,
                player2Id = match.Player2Id,
                player2Name = match.Player2Name,
                player2Score = match.Player2Score,
                player2Avg = match.Player2Avg,
                player2HighRun = match.Player2HighRun,
                inning = match.Inning,
                matchTarget = match.MatchTarget,
                winner = match.Winner,
                startedAt = match.StartedAt,
                endedAt = match.EndedAt,
                playedAt = match.PlayedAt
            };

            LogSendStats(match.Id, match.Player1Name, match.Player1Score, match.Player2Name, match.Player2Score, match.Winner);
            var response = await http.PostAsJsonAsync("stats", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            LogReceivedStatsResponse(match.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                LogPushStatsFailed(match.Id, response.StatusCode);
                continue;
            }

            match.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task CleanupSyncedMatchesAsync(DataContext db, CancellationToken ct)
    {
        var finished = await db.MatchResultSet.Where(m => m.SyncedWS).ToListAsync(ct);
        if (finished.Count == 0)
        {
            return;
        }

        var finishedIds = finished.Select(m => m.Id).ToList();
        var finishedStats = await db.MatchScoreStatSet.Where(s => finishedIds.Contains(s.MatchResultId)).ToListAsync(ct);
        db.MatchScoreStatSet.RemoveRange(finishedStats);
        db.MatchResultSet.RemoveRange(finished);
        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "RemoteSync is disabled (RemoteSync:Enabled=false) - skipping remote data push.")]
    private partial void LogSyncDisabled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RemoteSync is enabled but RemoteSync:BaseUrl is empty - skipping remote data push.")]
    private partial void LogSyncNoBaseUrl();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote sync tick failed; will retry next interval.")]
    private partial void LogSyncTickFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "RemoteSync could not start; remote data push is disabled for this run.")]
    private partial void LogSyncStartFailed(Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND team {TeamId} to teams: {Name}")]
    private partial void LogSendTeam(int teamId, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED response for team {TeamId}: {Status} {Body}")]
    private partial void LogReceivedTeamResponse(int teamId, System.Net.HttpStatusCode status, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push team {TeamId} failed: {Status}")]
    private partial void LogPushTeamFailed(int teamId, System.Net.HttpStatusCode status);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND player {PlayerId} to players: nickname={Nickname} name={Name} avatarId={AvatarId} photoBytes={PhotoBytes}")]
    private partial void LogSendPlayer(int playerId, string nickname, string name, int? avatarId, int photoBytes);

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED response for player {PlayerId}: {Status} {Body}")]
    private partial void LogReceivedPlayerResponse(int playerId, System.Net.HttpStatusCode status, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push player {PlayerId} failed: {Status}")]
    private partial void LogPushPlayerFailed(int playerId, System.Net.HttpStatusCode status);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND stats for match {MatchId} to stats: {Player1Name}({Player1Score}) vs {Player2Name}({Player2Score}) winner={Winner}")]
    private partial void LogSendStats(int matchId, string player1Name, int player1Score, string player2Name, int player2Score, int winner);

    [LoggerMessage(Level = LogLevel.Information, Message = "RECEIVED response for match {MatchId}: {Status} {Body}")]
    private partial void LogReceivedStatsResponse(int matchId, System.Net.HttpStatusCode status, string body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push stats for match {MatchId} failed: {Status}")]
    private partial void LogPushStatsFailed(int matchId, System.Net.HttpStatusCode status);
}
