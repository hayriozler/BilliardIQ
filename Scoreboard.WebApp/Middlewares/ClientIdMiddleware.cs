using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Middlewares;

public class ClientIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Client-Id";
    public const string ItemKey = "ClientId";
    public const string ClubIdItemKey = "ClubId";

    private static readonly PathString _apiPath = "/api";
    private static readonly PathString _clientsPath = "/api/clients";
    private static readonly PathString _clubsPath = "/api/clubs";

    public async Task InvokeAsync(HttpContext context, DataContext db)
    {
        if (!context.Request.Path.StartsWithSegments(_apiPath) ||
            context.Request.Path.StartsWithSegments(_clientsPath) ||
            context.Request.Path.StartsWithSegments(_clubsPath))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"'{HeaderName}' header is required." });
            return;
        }

        var clientId = values.ToString();
        var client = await db.ScoreboardClientSet.FindAsync(clientId);
        if (client is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unknown client id." });
            return;
        }

        client.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        context.Items[ItemKey] = clientId;
        context.Items[ClubIdItemKey] = client.ClubId;

        await next(context);
    }
}

public static class HttpContextClientIdExtensions
{
    public static string? GetClientId(this HttpContext context) =>
        context.Items.TryGetValue(ClientIdMiddleware.ItemKey, out var value) ? value as string : null;

    public static int? GetClubId(this HttpContext context) =>
        context.Items.TryGetValue(ClientIdMiddleware.ClubIdItemKey, out var value) ? value as int? : null;
}
