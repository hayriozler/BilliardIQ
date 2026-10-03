using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record ExternalMatchRequest(DateOnly PlayedOn, string OpponentName, string? Venue, int Score, int OpponentScore, int Innings, int HighRun, ExternalOutcome? Outcome);

public static class MobileExternalMatchEndpoints
{
    public static IEndpointRouteBuilder MapMobileExternalMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var own = app.MapGroup("/api/mobile/player/external-matches")
            .WithTags("MobileExternalMatches")
            .RequireAuthorization(JwtSettings.MobilePlayerPolicy);

        own.MapGet("/", (HttpContext context, ExternalMatchService matches) =>
            matches.ListAsync(context.User.GetPlayerId()));

        own.MapGet("/summary", (HttpContext context, ExternalMatchService matches) =>
            matches.SummaryAsync(context.User.GetPlayerId()));

        own.MapPost("/", async (ExternalMatchRequest request, HttpContext context, ExternalMatchService matches, Loc loc) =>
        {
            try
            {
                return Results.Ok(await matches.CreateAsync(context.User.GetPlayerId(), ToInput(request)));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        own.MapPut("/{id:int}", async (int id, ExternalMatchRequest request, HttpContext context, ExternalMatchService matches, Loc loc) =>
        {
            try
            {
                return await matches.UpdateAsync(context.User.GetPlayerId(), id, ToInput(request)) is { } updated
                    ? Results.Ok(updated)
                    : Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        own.MapDelete("/{id:int}", async (int id, HttpContext context, ExternalMatchService matches) =>
            await matches.DeleteAsync(context.User.GetPlayerId(), id) ? Results.NoContent() : Results.NotFound());

        var manage = app.MapGroup("/api/mobile/manage/external-matches")
            .WithTags("MobileExternalMatches")
            .RequireAuthorization(JwtSettings.MobileManagerPolicy);

        manage.MapGet("/", (ExternalMatchService matches, int? playerId) => matches.ListAsync(playerId));

        return app;
    }

    private static ExternalMatchInput ToInput(ExternalMatchRequest r) =>
        new(r.PlayedOn, r.OpponentName, r.Venue, r.Score, r.OpponentScore, r.Innings, r.HighRun, r.Outcome);
}
