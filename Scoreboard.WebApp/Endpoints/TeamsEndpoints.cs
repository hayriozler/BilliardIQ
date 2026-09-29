using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Models;
using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams");

        group.MapGet("/", async (TeamService teams, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            return Results.Ok((await teams.ListForClubAsync(clubId.Value)).Select(ToDto).ToList());
        });

        group.MapPost("/", async (UpsertTeamRequest request, TeamService teams, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            var team = await teams.UpsertAsync(clubId.Value, request.Id, request.Name);
            return Results.Ok(ToDto(team));
        });

        group.MapDelete("/{id:int}", async (int id, TeamService teams, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            return await teams.DeleteAsync(clubId.Value, id) ? Results.NoContent() : Results.NotFound();
        });

        group.MapPut("/{id:int}/players", async (int id, SetTeamPlayersRequest request, TeamService teams, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            try
            {
                var team = await teams.SetPlayersAsync(clubId.Value, id, request.PlayerIds);
                return team is null ? Results.NotFound() : Results.Ok(ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}/players/{playerId:int}", async (int id, int playerId, TeamService teams, HttpContext context) =>
        {
            var clubId = context.GetClubId();
            if (clubId is null)
            {
                return Results.BadRequest("Client is not linked to a club.");
            }

            var team = await teams.RemovePlayerAsync(clubId.Value, id, playerId);
            return team is null ? Results.NotFound() : Results.Ok(ToDto(team));
        });

        return group;
    }

    private static TeamDto ToDto(Team team) => new(
        team.Id, team.ClubId, team.Name, team.UpdatedAt,
        team.TeamPlayers.Select(tp => new TeamPlayerDto(tp.Player!.Id, tp.Player.Nickname, tp.Player.Name)).ToList());
}
