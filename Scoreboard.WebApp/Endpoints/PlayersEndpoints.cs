using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players");

        group.MapGet("/", async (PlayerService players, HttpContext context) =>
            (await players.ListForOrganizationAsync(context.GetOrganizationId())).Select(ToDto).ToList());

        group.MapPost("/", async (UpsertPlayerRequest request, PlayerService players, HttpContext context) =>
        {
            try
            {
                var player = await players.UpsertAsync(
                    context.GetOrganizationId(), request.Id, request.Nickname, request.Name, request.AvatarId,
                    request.Email, request.Level, request.BaseCountry, request.BaseCity,
                    request.PhotoBase64, request.PhotoExtension, request.ShortcutNumber,
                    request.LicenseNo, request.LicenseValidUntil, request.AssociationId, request.RegionId, request.CountryId, request.CityId);
                return Results.Ok(ToDto(player));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, PlayerService players, HttpContext context) =>
        {
            try
            {
                return await players.DeleteAsync(context.GetOrganizationId(), id) ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        });

        return group;
    }

    private static PlayerDto ToDto(Player p) => new(
        p.Id, null, p.Nickname ?? p.DisplayName, $"{p.FirstName} {p.LastName}".Trim(), p.PhotoUrl, p.AvatarId,
        p.Email ?? string.Empty, p.Level, p.Nationality ?? string.Empty, p.City ?? string.Empty, p.UpdatedAt, p.ShortcutNumber,
        p.FederationLicenseNo, p.LicenseValidUntil, p.Association?.Name, p.IsSystem, p.SystemSlot, p.AssociationId, p.RegionId, p.Region?.Name, p.CountryId, p.CityId);
}
