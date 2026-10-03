using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Responses;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record MobileTable(int Id, int Number, int? ScoreboardNo, string? Label, TableType Type, TableStatus Status, DateTimeOffset? SessionOpenedAt);

public static class MobileManageEndpoints
{
    public static IEndpointRouteBuilder MapMobileManageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/manage")
            .WithTags("MobileManage")
            .RequireAuthorization(JwtSettings.MobileManagerPolicy);

        group.MapGet("/tables", async (TableService tables) =>
            (await tables.ListAsync()).Select(v => new MobileTable(
                v.Table.Id, v.Table.Number, v.Table.ScoreboardNo, v.Table.Label, v.Table.Type, v.Table.Status, v.Session?.OpenedAt)));

        group.MapGet("/players", (ScoreboardDataService data) => data.ListPlayersAsync());

        group.MapGet("/teams", (ScoreboardDataService data) => data.ListTeamsAsync());

        return app;
    }
}
