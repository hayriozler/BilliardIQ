using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class ScoreboardEndpoints
{
    public static RouteGroupBuilder MapScoreboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scoreboard").WithTags("Scoreboard");

        group.MapGet("/organization", (ScoreboardDataService data) =>
            data.GetOrganizationAsync());
        group.MapGet("/clubs", (ScoreboardDataService data) =>
            data.ListClubsAsync());
        group.MapGet("/players", (ScoreboardDataService data) =>
            data.ListPlayersAsync());
        group.MapGet("/teams", (ScoreboardDataService data) =>
            data.ListTeamsAsync());

        return group;
    }
}
