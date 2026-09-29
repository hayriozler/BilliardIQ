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

        group.MapGet("/", async (ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            return await db.MatchStatSet
                .Where(s => s.ClientId == clientId)
                .OrderByDescending(s => s.PlayedAt)
                .Select(s => ToDto(s))
                .ToListAsync();
        });

        group.MapPost("/", async (SubmitMatchStatRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;

            var stat = new MatchStat
            {
                ClientId = clientId,
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
                PlayedAt = request.PlayedAt
            };

            db.MatchStatSet.Add(stat);
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(stat));
        });

        group.MapDelete("/{id:int}", async (int id, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var stat = await db.MatchStatSet.FirstOrDefaultAsync(s => s.Id == id && s.ClientId == clientId);
            if (stat is null)
            {
                return Results.NotFound();
            }

            db.MatchStatSet.Remove(stat);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private static MatchStatDto ToDto(MatchStat s) => new(
        s.Id, s.ClientId,
        s.Player1ExternalId, s.Player1Name, s.Player1Score, s.Player1Avg, s.Player1HighRun,
        s.Player2ExternalId, s.Player2Name, s.Player2Score, s.Player2Avg, s.Player2HighRun,
        s.Inning, s.MatchTarget, s.Winner, s.PlayedAt, s.RecordedAt);
}
