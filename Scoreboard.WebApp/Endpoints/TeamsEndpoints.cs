using Scoreboard.WebApp.Requests;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams").RequireAuthorization(AuthClaims.StaffPolicy);
        var write = group.MapGroup("").RequireAuthorization(AuthClaims.ManagePolicy);

        group.MapGet("/", (ScoreboardDataService data) =>
            data.ListTeamsAsync());

        write.MapPost("/", async (UpsertTeamRequest request, TeamService teams) =>
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

        write.MapDelete("/{id:int}", async (int id, TeamService teams) =>
            await teams.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        write.MapPut("/{id:int}/players", async (int id, SetTeamPlayersRequest request, TeamService teams) =>
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

        write.MapDelete("/{id:int}/players/{playerId:int}", async (int id, int playerId, TeamService teams) =>
        {
            var team = await teams.RemovePlayerAsync(id, playerId);
            return team is null ? Results.NotFound() : Results.Ok(ScoreboardDataService.ToDto(team));
        });

        return group;
    }
}
