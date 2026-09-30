using Scoreboard.WebApp.Middlewares;
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
            (await teams.ListForOrganizationAsync(context.GetOrganizationId())).Select(ToDto).ToList());

        group.MapPost("/", async (UpsertTeamRequest request, TeamService teams, HttpContext context) =>
        {
            try
            {
                var team = await teams.UpsertAsync(context.GetOrganizationId(), request.Id, request.Name, request.ClubId, request.AvatarId);
                return Results.Ok(ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, TeamService teams, HttpContext context) =>
            await teams.DeleteAsync(context.GetOrganizationId(), id) ? Results.NoContent() : Results.NotFound());

        group.MapPut("/{id:int}/players", async (int id, SetTeamPlayersRequest request, TeamService teams, HttpContext context) =>
        {
            try
            {
                var team = await teams.SetPlayersAsync(context.GetOrganizationId(), id, request.PlayerIds);
                return team is null ? Results.NotFound() : Results.Ok(ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}/players/{playerId:int}", async (int id, int playerId, TeamService teams, HttpContext context) =>
        {
            var team = await teams.RemovePlayerAsync(context.GetOrganizationId(), id, playerId);
            return team is null ? Results.NotFound() : Results.Ok(ToDto(team));
        });

        return group;
    }

    private static TeamDto ToDto(Team team) => new(
        team.Id, team.ClubId, team.Name, team.UpdatedAt,
        team.Members.Select(m => new TeamPlayerDto(
            m.Player.Id, m.Player.Nickname ?? m.Player.DisplayName, $"{m.Player.FirstName} {m.Player.LastName}".Trim())).ToList(),
        team.AvatarId);
}
