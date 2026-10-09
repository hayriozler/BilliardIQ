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

        group.MapGet("/changes", async (HttpContext context, ScoreboardDataService data, bool? resync) =>
        {
            if (InstanceId(context) is not { } instanceId)
            {
                return Results.BadRequest(new { error = $"'{InstanceHeader}' header is required." });
            }

            return Results.Ok(await data.GetChangesAsync(instanceId, TableNo(context), resync == true, ClientIp(context)));
        });

        group.MapPost("/changes/ack", async (HttpContext context, ScoreboardDataService data) =>
        {
            if (InstanceId(context) is not { } instanceId)
            {
                return Results.BadRequest(new { error = $"'{InstanceHeader}' header is required." });
            }

            await data.AckChangesAsync(instanceId, TableNo(context));
            return Results.NoContent();
        });

        return group;
    }

    private const string InstanceHeader = "X-Client-Instance";
    private const string IpHeader = "X-Client-Ip";

    private static string? InstanceId(HttpContext context)
    {
        var value = context.Request.Headers[InstanceHeader].ToString().Trim();
        return value.Length is > 0 and <= 40 ? value : null;
    }

    private static string? ClientIp(HttpContext context) =>
        System.Net.IPAddress.TryParse(context.Request.Headers[IpHeader].ToString().Trim(), out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? ip.ToString()
            : null;

    private static int TableNo(HttpContext context) =>
        int.TryParse(context.Request.Headers[ClientIdMiddleware.TableHeaderName].ToString(), out var tableNo) && tableNo > 0 ? tableNo : 0;
}
