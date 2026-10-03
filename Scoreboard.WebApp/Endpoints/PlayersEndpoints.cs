using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players").RequireAuthorization();
        var write = group.MapGroup("").RequireAuthorization(AuthClaims.ManagePolicy);

        group.MapGet("/", (ScoreboardDataService data) =>
            data.ListPlayersAsync());

        write.MapPost("/", async (UpsertPlayerRequest request, PlayerService players, ScoreboardDataService data) =>
        {
            try
            {
                var player = await players.UpsertAsync(request.Id, request.Nickname, request.Name, request.AvatarId,
                    request.Email, request.Level, request.BaseCountry, request.BaseCity,
                    request.PhotoBase64, request.ShortcutNumber,
                    request.LicenseNo, request.LicenseValidUntil, request.AssociationId, request.RegionId, request.CountryId, request.CityId);
                return Results.Ok(ScoreboardDataService.ToDto(player, await data.LanguageOfAsync()));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        write.MapDelete("/{id:int}", async (int id, PlayerService players) =>
        {
            try
            {
                return await players.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        return group;
    }
}
