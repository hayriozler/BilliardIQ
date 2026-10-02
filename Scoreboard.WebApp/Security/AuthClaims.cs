using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Scoreboard.WebApp.Security;

public static class AuthClaims
{
    public const string OrganizationId = "OrganizationId";
    public const string StaffMemberId = "StaffMemberId";
    public const string OrganizationName = "OrganizationName";
    public const string SecurityStamp = "SecurityStamp";
    public const string ManagePolicy = "CanManage";

    public static ClaimsPrincipal CreatePrincipal(User user, StaffMember staff, Organization organization, string scheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(OrganizationId, organization.Id.ToString()),
            new(OrganizationName, organization.Name),
            new(StaffMemberId, staff.Id.ToString()),
            new(SecurityStamp, user.SecurityStamp)
        };
        claims.AddRange(staff.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }

    public static int GetOrganizationId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(OrganizationId)!.Value);

    public static int GetStaffMemberId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(StaffMemberId)!.Value);

    public static int GetUserId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static bool CanManage(this ClaimsPrincipal user) =>
        user.IsInRole(nameof(StaffRole.Owner)) || user.IsInRole(nameof(StaffRole.Manager));
}

public class TenantContext(AuthenticationStateProvider authState)
{
    private Tenant? _tenant;

    public async Task<Tenant> GetAsync()
    {
        if (_tenant is not null)
        {
            return _tenant;
        }

        var user = (await authState.GetAuthenticationStateAsync()).User;
        return _tenant = new Tenant(user.GetOrganizationId(), user.GetStaffMemberId(), user.GetUserId(), user.CanManage(), user.Identity?.Name ?? "", user.IsInRole(nameof(StaffRole.Owner)));
    }
}

public record Tenant(int OrganizationId, int StaffMemberId, int UserId, bool CanManage, string DisplayName, bool IsOwner = false);
