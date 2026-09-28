using Zeymera.BillardIQ.WebApp.Middlewares;
using Zeymera.BillardIQ.WebApp.Models;
using Zeymera.BillardIQ.WebApp.Requests;
using Zeymera.BillardIQ.WebApp.Responses;
using Zeymera.BillardIQ.WebApp.Services;

namespace Zeymera.BillardIQ.WebApp.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players");

        group.MapGet("/", async (PlayerService players, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            return Results.Ok((await players.ListForClubAsync(clubId.Value)).Select(ToDto).ToList());
        });

        group.MapPost("/", async (UpsertPlayerRequest request, PlayerService players, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            try
            {
                var player = await players.UpsertAsync(
                    clubId.Value, request.Id, request.Nickname, request.Name, request.AvatarId,
                    request.Email, request.Level, request.BaseCountry, request.BaseCity,
                    request.PhotoBase64, request.PhotoExtension);
                return Results.Ok(ToDto(player));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, PlayerService players, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            return await players.DeleteAsync(clubId.Value, id) ? Results.NoContent() : Results.NotFound();
        });

        return group;
    }

    private static PlayerDto ToDto(Player p) =>
        new(p.Id, p.ClubId, p.Nickname, p.Name, p.PhotoPath, p.AvatarId, p.Email, p.Level, p.BaseCountry, p.BaseCity, p.UpdatedAt);
}
