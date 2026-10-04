using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class OrganizationEndpoints
{
    public static RouteGroupBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization").WithTags("Organization").RequireAuthorization(AuthClaims.StaffPolicy);

        group.MapGet("/", (ScoreboardDataService data) =>
            data.GetOrganizationAsync());

        return group;
    }
}
