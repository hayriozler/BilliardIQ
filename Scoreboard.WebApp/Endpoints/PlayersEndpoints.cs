using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players").RequireAuthorization();

        group.MapGet("/", (ScoreboardDataService data) =>
            data.ListPlayersAsync());

        group.MapPost("/", async (UpsertPlayerRequest request, PlayerService players, ScoreboardDataService data) =>
        {
            try
            {
                var player = await players.UpsertAsync(request.Id, request.Nickname, request.Name, request.AvatarId,
                    request.Email, request.Level, request.BaseCountry, request.BaseCity,
                    request.PhotoBase64, request.PhotoExtension, request.ShortcutNumber,
                    request.LicenseNo, request.LicenseValidUntil, request.AssociationId, request.RegionId, request.CountryId, request.CityId);
                return Results.Ok(ScoreboardDataService.ToDto(player, await data.LanguageOfAsync()));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, PlayerService players) =>
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
