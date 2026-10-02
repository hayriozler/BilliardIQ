using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams").RequireAuthorization();

        group.MapGet("/", (ScoreboardDataService data) =>
            data.ListTeamsAsync());

        group.MapPost("/", async (UpsertTeamRequest request, TeamService teams) =>
        {
            try
            {
                var team = await teams.UpsertAsync(request.Id, request.Name, request.ClubId, request.AvatarId);
                return Results.Ok(ScoreboardDataService.ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}", async (int id, TeamService teams) =>
            await teams.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        group.MapPut("/{id:int}/players", async (int id, SetTeamPlayersRequest request, TeamService teams) =>
        {
            try
            {
                var team = await teams.SetPlayersAsync(id, request.PlayerIds);
                return team is null ? Results.NotFound() : Results.Ok(ScoreboardDataService.ToDto(team));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/{id:int}/players/{playerId:int}", async (int id, int playerId, TeamService teams) =>
        {
            var team = await teams.RemovePlayerAsync(id, playerId);
            return team is null ? Results.NotFound() : Results.Ok(ScoreboardDataService.ToDto(team));
        });

        return group;
    }
}
