using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Models;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class MatchStatsEndpoints
{
    private const int MaxNameLength = 100;
    private const int MaxTarget = 1000;
    private const int MaxHistoryRows = 5000;
    private const int HistoryBucketMinutes = 5;

    public static RouteGroupBuilder MapMatchStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stats").WithTags("MatchStats");

        group.MapGet("/", async (DataContext db, HttpContext context) =>
        {
            var tableId = context.GetTableId();
            var stats = await db.MatchStatSet
                .Include(s => s.Buckets)
                .Where(s => tableId == null || s.TableId == tableId)
                .OrderByDescending(s => s.PlayedAt)
                .Take(200)
                .ToListAsync();
            return stats.Select(ToDto).ToList();
        });

        group.MapPost("/", async (SubmitMatchStatRequest request, DataContext db, HttpContext context) =>
        {
            if (context.GetTableId() is not { } tableId)
            {
                return Results.BadRequest(new { error = $"'{ClientIdMiddleware.TableHeaderName}' header is required." });
            }

            var history = (request.History ?? [])
                .Take(MaxHistoryRows)
                .Where(h => h.PlayerSlot is 1 or 2)
                .Select(h => new MatchStatHistory
                {
                    PlayerId = h.PlayerId,
                    PlayerSlot = h.PlayerSlot,
                    Inning = Math.Clamp(h.Inning, 0, MaxTarget),
                    Score = Math.Clamp(h.Score, -MaxTarget, MaxTarget),
                    TotalScore = Math.Clamp(h.TotalScore, 0, MaxTarget * 10),
                    PlayedAt = h.PlayedAt?.ToUniversalTime()
                })
                .ToList();

            var buckets = BucketsFromHistory(history, request.StartedAt?.ToUniversalTime());

            var stat = new MatchStat
            {
                OrganizationId = db.CurrentOrganizationId,
                TableId = tableId,
                TableNo = context.GetTableNo(),
                Player1ExternalId = request.Player1Id,
                Player1Name = ClampName(request.Player1Name),
                Player1Score = request.Player1Score,
                Player1Avg = request.Player1Avg,
                Player1HighRun = request.Player1HighRun,
                Player2ExternalId = request.Player2Id,
                Player2Name = ClampName(request.Player2Name),
                Player2Score = request.Player2Score,
                Player2Avg = request.Player2Avg,
                Player2HighRun = request.Player2HighRun,
                Inning = request.Inning,
                MatchTarget = request.MatchTarget,
                IsHandicap = request.IsHandicap,
                Player1Target = request.IsHandicap ? Math.Clamp(request.Player1Target, 0, MaxTarget) : 0,
                Player2Target = request.IsHandicap ? Math.Clamp(request.Player2Target, 0, MaxTarget) : 0,
                Winner = request.Winner,
                PlayedAt = request.PlayedAt.ToUniversalTime(),
                StartedAt = request.StartedAt?.ToUniversalTime(),
                EndedAt = request.EndedAt?.ToUniversalTime(),
                BucketMinutes = buckets.Count == 0 ? 0 : HistoryBucketMinutes,
                Buckets = buckets,
                History = history
            };

            db.MatchStatSet.Add(stat);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(stat));
        });

        return group;
    }

    private static List<MatchStatBucket> BucketsFromHistory(List<MatchStatHistory> history, DateTimeOffset? startedAt)
    {
        var timed = history.Where(h => h.PlayedAt is not null).ToList();
        if (timed.Count == 0)
        {
            return [];
        }

        var start = startedAt ?? timed.Min(h => h.PlayedAt!.Value);
        return [.. timed
            .GroupBy(h => (h.PlayerSlot, BucketIndex: Math.Max(0, (int)((h.PlayedAt!.Value - start).TotalMinutes / HistoryBucketMinutes))))
            .Where(g => g.Key.BucketIndex <= StatsService.MaxBucketIndex)
            .Select(g => new MatchStatBucket
            {
                PlayerSlot = g.Key.PlayerSlot,
                BucketIndex = g.Key.BucketIndex,
                TotalPoints = g.Sum(h => h.Score)
            })];
    }

    private static string ClampName(string? name)
    {
        var trimmed = (name ?? "").Trim();
        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }

    private static MatchStatDto ToDto(MatchStat s) => new(
        s.Id, s.TableNo,
        s.Player1ExternalId, s.Player1Name, s.Player1Score, s.Player1Avg, s.Player1HighRun,
        s.Player2ExternalId, s.Player2Name, s.Player2Score, s.Player2Avg, s.Player2HighRun,
        s.Inning, s.MatchTarget, s.IsHandicap, s.Player1Target, s.Player2Target, s.Winner, s.PlayedAt, s.StartedAt, s.EndedAt, s.RecordedAt,
        s.BucketMinutes,
        s.Buckets.OrderBy(b => b.PlayerSlot).ThenBy(b => b.BucketIndex)
            .Select(b => new ScoreBucketDto(b.PlayerSlot, b.BucketIndex, b.TotalPoints)).ToList());
}
