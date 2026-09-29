using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Scoreboard.Client.Services;

public partial class RemoteWsPushService(
    IDbContextFactory<DataContext> dbFactory,
    ScoreboardCommandHub hub,
    SystemPowerService systemPower,
    ILogger<RemoteWsPushService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var interval = TimeSpan.FromSeconds(5);
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await PushOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "WS push tick failed; will retry next interval.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PushOnceAsync(CancellationToken ct)
    {
        if (!hub.HasActiveConnection || systemPower.IsShuttingDown)
        {
            return;
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await PushTeamsAsync(db, ct);
        await PushPlayersAsync(db, ct);
        await PushStatsAsync(db, ct);
    }

    private async Task PushTeamsAsync(DataContext db, CancellationToken ct)
    {
        var pending = await db.TeamSet.Where(t => !t.SyncedWS).ToListAsync(ct);
        foreach (var team in pending)
        {
            var json = JsonSerializer.Serialize(new
            {
                type = "teams",
                team = new { id = team.Id, name = team.Name }
            }, _jsonOptions);

            LogSendTeam(team.Id, json);
            await hub.BroadcastJsonAsync(json);
            team.SyncedWS = true;
            await db.SaveChangesAsync(ct);
        }
    }
    private async Task PushPlayersAsync(DataContext db, CancellationToken ct)
    {
        var pending = await db.PlayerSet.Where(p => !p.SyncedWS).ToListAsync(ct);
        foreach (var player in pending)
        {
            var json = JsonSerializer.Serialize(new
            {
                type = "players",
                player = new
                {
                    id = player.Id,
                    nickname = player.Nickname,
                    name = player.Name,
                    avatarId = player.AvatarId,
                    teamId = player.TeamId,
                    shortcutNumber = player.ShortcutNumber
                }
            }, _jsonOptions);

            LogSendPlayer(player.Id, json);
            await hub.BroadcastJsonAsync(json);
            player.SyncedWS = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushStatsAsync(DataContext db, CancellationToken ct)
    {
        var pending = await db.MatchResultSet.Where(m => !m.SyncedWS).ToListAsync(ct);
        foreach (var match in pending)
        {
            var scoreDistribution = await db.MatchScoreStatSet
                .Where(s => s.MatchResultId == match.Id)
                .OrderBy(s => s.PlayerSlot).ThenBy(s => s.BucketIndex)
                .Select(s => new { playerSlot = s.PlayerSlot, bucketIndex = s.BucketIndex, totalPoints = s.TotalPoints })
                .ToListAsync(ct);

            var json = JsonSerializer.Serialize(new
            {
                type = "matchResult",
                matchResult = new
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
                    playedAt = match.PlayedAt,
                    startedAt = match.StartedAt,
                    endedAt = match.EndedAt,
                    scoreDistributionBucketMinutes = 5,
                    scoreDistribution
                }
            }, _jsonOptions);

            LogSendMatchResult(match.Id, json);
            await hub.BroadcastJsonAsync(json);
            match.SyncedWS = true;
            await db.SaveChangesAsync(ct);
        }

        var finished = await db.MatchResultSet.Where(m => m.SyncedWS).ToListAsync(ct);
        if (finished.Count > 0)
        {
            var finishedIds = finished.Select(m => m.Id).ToList();
            var finishedStats = await db.MatchScoreStatSet.Where(s => finishedIds.Contains(s.MatchResultId)).ToListAsync(ct);
            db.MatchScoreStatSet.RemoveRange(finishedStats);
            db.MatchResultSet.RemoveRange(finished);
            await db.SaveChangesAsync(ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND WS team {TeamId}: {Json}")]
    private partial void LogSendTeam(int teamId, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND WS player {PlayerId}: {Json}")]
    private partial void LogSendPlayer(int playerId, string json);

    [LoggerMessage(Level = LogLevel.Information, Message = "SEND WS matchResult {MatchId}: {Json}")]
    private partial void LogSendMatchResult(int matchId, string json);
}
