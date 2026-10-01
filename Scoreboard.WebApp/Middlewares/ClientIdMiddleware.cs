using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Middlewares;

/// <summary>
/// Identifies a kiosk's salon (organization) from the X-Client-Id header, and optionally its table from X-Table-No.
/// There is no other authentication: the client id is the organization's shared kiosk identifier.
/// </summary>
public class ClientIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Client-Id";
    public const string TableHeaderName = "X-Table-No";
    public const string OrganizationIdItemKey = "OrganizationId";
    public const string TableIdItemKey = "TableId";
    public const string TableNoItemKey = "TableNo";

    private static readonly PathString[] _clientPaths = ["/api/scoreboard", "/api/stats"];

    public async Task InvokeAsync(HttpContext context, DataContext db, OrganizationScope scope)
    {
        if (!_clientPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            await next(context);
            return;
        }

        var clientId = context.Request.Headers[HeaderName].ToString().Trim();
        if (clientId.Length == 0)
        {
            await RejectAsync(context, StatusCodes.Status400BadRequest, $"'{HeaderName}' header is required.");
            return;
        }

        var organization = await db.OrganizationSet
            .Where(o => o.ClientId == clientId && o.DeletedAt == null && o.IsActive)
            .Select(o => new { o.Id })
            .FirstOrDefaultAsync();
        if (organization is null)
        {
            await RejectAsync(context, StatusCodes.Status401Unauthorized, "Unknown client id.");
            return;
        }

        context.Items[OrganizationIdItemKey] = organization.Id;
        scope.OrganizationId = organization.Id;

        var tableHeader = context.Request.Headers[TableHeaderName].ToString().Trim();
        if (tableHeader.Length > 0)
        {
            if (!int.TryParse(tableHeader, out var tableNo))
            {
                await RejectAsync(context, StatusCodes.Status400BadRequest, $"'{TableHeaderName}' must be a table number.");
                return;
            }

            var table = await db.BilliardTableSet
                .Where(t => t.OrganizationId == organization.Id && t.ScoreboardNo == tableNo && t.DeletedAt == null)
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync();
            if (table is null)
            {
                await RejectAsync(context, StatusCodes.Status404NotFound, $"No scoreboard table with number {tableNo}.");
                return;
            }

            context.Items[TableIdItemKey] = table.Id;
            context.Items[TableNoItemKey] = tableNo;
        }

        await next(context);
    }

    private static Task RejectAsync(HttpContext context, int status, string error)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { error });
    }
}

public class OrganizationScope
{
    public int? OrganizationId { get; set; }
}

public static class HttpContextClientIdExtensions
{
    public static int GetOrganizationId(this HttpContext context) =>
        context.Items[ClientIdMiddleware.OrganizationIdItemKey] as int?
        ?? context.User.GetOrganizationId();

    /// <summary>Table id resolved from X-Table-No, or null when the header was not sent.</summary>
    public static int? GetTableId(this HttpContext context) => context.Items[ClientIdMiddleware.TableIdItemKey] as int?;

    public static int? GetTableNo(this HttpContext context) => context.Items[ClientIdMiddleware.TableNoItemKey] as int?;
}
