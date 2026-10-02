using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Models;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;

namespace Scoreboard.WebApp.Endpoints;

public static class MatchStatsEndpoints
{
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

            var buckets = (request.ScoreDistribution ?? [])
                .Where(b => b.PlayerSlot is 1 or 2 && b.BucketIndex >= 0)
                .GroupBy(b => (b.PlayerSlot, b.BucketIndex))
                .Select(g => new MatchStatBucket
                {
                    PlayerSlot = g.Key.PlayerSlot,
                    BucketIndex = g.Key.BucketIndex,
                    TotalPoints = g.Sum(b => b.TotalPoints)
                })
                .ToList();

            var stat = new MatchStat
            {
                OrganizationId = db.CurrentOrganizationId,
                TableId = tableId,
                TableNo = context.GetTableNo(),
                Player1ExternalId = request.Player1Id,
                Player1Name = request.Player1Name,
                Player1Score = request.Player1Score,
                Player1Avg = request.Player1Avg,
                Player1HighRun = request.Player1HighRun,
                Player2ExternalId = request.Player2Id,
                Player2Name = request.Player2Name,
                Player2Score = request.Player2Score,
                Player2Avg = request.Player2Avg,
                Player2HighRun = request.Player2HighRun,
                Inning = request.Inning,
                MatchTarget = request.MatchTarget,
                Winner = request.Winner,
                PlayedAt = request.PlayedAt,
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                BucketMinutes = buckets.Count == 0 ? 0 : Math.Max(1, request.ScoreDistributionBucketMinutes),
                Buckets = buckets
            };

            db.MatchStatSet.Add(stat);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(stat));
        });

        group.MapDelete("/{id:int}", async (int id, DataContext db, HttpContext context) =>
        {
            var stat = await db.MatchStatSet.Include(s => s.Buckets)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (stat is null)
            {
                return Results.NotFound();
            }

            db.MatchStatBucketSet.RemoveRange(stat.Buckets);
            db.MatchStatSet.Remove(stat);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private static MatchStatDto ToDto(MatchStat s) => new(
        s.Id, s.TableNo,
        s.Player1ExternalId, s.Player1Name, s.Player1Score, s.Player1Avg, s.Player1HighRun,
        s.Player2ExternalId, s.Player2Name, s.Player2Score, s.Player2Avg, s.Player2HighRun,
        s.Inning, s.MatchTarget, s.Winner, s.PlayedAt, s.StartedAt, s.EndedAt, s.RecordedAt,
        s.BucketMinutes,
        s.Buckets.OrderBy(b => b.PlayerSlot).ThenBy(b => b.BucketIndex)
            .Select(b => new ScoreBucketDto(b.PlayerSlot, b.BucketIndex, b.TotalPoints)).ToList());
}
