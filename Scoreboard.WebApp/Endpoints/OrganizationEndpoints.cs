using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Responses;

namespace Scoreboard.WebApp.Endpoints;

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization").WithTags("Organization");

        group.MapGet("/", async (DataContext db, HttpContext context) =>
        {
            var organizationId = context.GetOrganizationId();
            var organization = await db.OrganizationSet.AsNoTracking().FirstAsync(o => o.Id == organizationId);
            return new OrganizationDto(organization.Name, organization.Language, organization.CountryCode, organization.Currency, organization.TimeZone);
        });

        return group;
    }
}
