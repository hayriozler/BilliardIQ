using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Security;

namespace Scoreboard.WebApp.Services;

public interface IOrganizationService
{
    int? GetCurrentOrganizationId();
}

public class OrganizationService(IHttpContextAccessor httpContextAccessor) : IOrganizationService
{
    public int? Override { get; set; }

    public int? GetCurrentOrganizationId()
    {
        if (Override is { } overridden)
        {
            return overridden;
        }

        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (context.Items[ClientIdMiddleware.OrganizationIdItemKey] is int clientOrganizationId)
        {
            return clientOrganizationId;
        }

        return context.User.Identity?.IsAuthenticated == true && context.User.FindFirst(AuthClaims.OrganizationId) is { } claim
            ? int.Parse(claim.Value)
            : null;
    }
}
