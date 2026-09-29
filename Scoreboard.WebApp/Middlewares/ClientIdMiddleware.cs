using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Middlewares;

/// <summary>
/// Identifies a kiosk by the pairing code it sends in X-Client-Id and resolves the device and its salon (organization).
/// The admin-facing /api/devices routes use cookie authentication instead.
/// </summary>
public class ClientIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Client-Id";
    public const string ItemKey = "ClientId";
    public const string DeviceIdItemKey = "DeviceId";
    public const string OrganizationIdItemKey = "OrganizationId";

    private static readonly PathString _apiPath = "/api";
    private static readonly PathString _devicesPath = "/api/devices";

    public async Task InvokeAsync(HttpContext context, DataContext db)
    {
        if (!context.Request.Path.StartsWithSegments(_apiPath) ||
            context.Request.Path.StartsWithSegments(_devicesPath))
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

        var clientId = values.ToString().Trim();
        var device = await db.DeviceSet.FirstOrDefaultAsync(d => d.PairingCode == clientId && d.DeletedAt == null);
        if (device is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unknown client id." });
            return;
        }

        device.LastSeenAt = DateTimeOffset.UtcNow;
        device.IsOnline = true;
        await db.SaveChangesAsync();

        context.Items[ItemKey] = clientId;
        context.Items[DeviceIdItemKey] = device.Id;
        context.Items[OrganizationIdItemKey] = device.OrganizationId;

        await next(context);
    }
}

public static class HttpContextClientIdExtensions
{
    public static string GetClientId(this HttpContext context) => (string)context.Items[ClientIdMiddleware.ItemKey]!;

    public static int GetDeviceId(this HttpContext context) => (int)context.Items[ClientIdMiddleware.DeviceIdItemKey]!;

    public static int GetOrganizationId(this HttpContext context) => (int)context.Items[ClientIdMiddleware.OrganizationIdItemKey]!;
}
