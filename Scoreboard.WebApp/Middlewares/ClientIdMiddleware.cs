using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Middlewares;

public class ClientIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Client-Id";
    public const string TableHeaderName = "X-Table-No";
    public const string OrganizationIdItemKey = "OrganizationId";
    public const string TableIdItemKey = "TableId";
    public const string TableNoItemKey = "TableNo";

    private static readonly PathString _statsPath = "/api/stats";
    private static readonly PathString[] _clientPaths = ["/api/scoreboard", "/api/stats"];

    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory, OrganizationRunner runner)
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

        using var lookupScope = scopeFactory.CreateScope();
        var db = lookupScope.ServiceProvider.GetRequiredService<DataContext>();

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

        var tableHeader = context.Request.Headers[TableHeaderName].ToString().Trim();
        if (tableHeader.Length > 0 && context.Request.Path.StartsWithSegments(_statsPath))
        {
            if (!int.TryParse(tableHeader, out var tableNo))
            {
                await RejectAsync(context, StatusCodes.Status400BadRequest, $"'{TableHeaderName}' must be a table number.");
                return;
            }

            var table = await runner.RunAsync<DataContext, int?>(organization.Id, tables =>
                tables.BilliardTableSet
                    .Where(t => t.ScoreboardNo == tableNo && t.DeletedAt == null)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync());
            if (table is null)
            {
                await RejectAsync(context, StatusCodes.Status404NotFound, $"No scoreboard table with number {tableNo}.");
                return;
            }

            context.Items[TableIdItemKey] = table.Value;
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

public static class HttpContextClientIdExtensions
{
    public static int GetOrganizationId(this HttpContext context) =>
        context.Items[ClientIdMiddleware.OrganizationIdItemKey] as int?
        ?? context.User.GetOrganizationId();

    public static int? GetTableId(this HttpContext context) => context.Items[ClientIdMiddleware.TableIdItemKey] as int?;

    public static int? GetTableNo(this HttpContext context) => context.Items[ClientIdMiddleware.TableNoItemKey] as int?;
}
