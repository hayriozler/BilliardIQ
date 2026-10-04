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
    public const string StaffPolicy = "Staff";
    public const string StatsPolicy = "StatsViewer";
    public const string PlayerAccountPolicy = "PlayerAccount";
    public const string PlayerHome = "/stats/players";
    public const string PlayerAccountHome = "/account";

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

    public static ClaimsPrincipal CreatePlayerPrincipal(User user, Player player, Organization organization, string scheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(OrganizationId, organization.Id.ToString()),
            new(OrganizationName, organization.Name),
            new(MobileClaims.PlayerId, player.Id.ToString()),
            new(SecurityStamp, user.SecurityStamp),
            new(ClaimTypes.Role, MobileClaims.Player)
        };
        if (user.MustChangePassword)
        {
            claims.Add(new Claim(MobileClaims.MustChangePassword, "1"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }

    public static int GetOrganizationId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(OrganizationId)!.Value);

    public static int GetStaffMemberId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(StaffMemberId)!.Value);

    public static int GetUserId(this ClaimsPrincipal user) => int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.FindFirst(StaffMemberId) is not null && !user.IsInRole(MobileClaims.Player);

    public static bool IsPlayer(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && user.FindFirst(StaffMemberId) is null && user.IsInRole(MobileClaims.Player)
        && user.FindFirst(MobileClaims.PlayerId) is not null && user.FindFirst(OrganizationId) is not null;

    public static bool MustChangePassword(this ClaimsPrincipal user) => user.FindFirst(MobileClaims.MustChangePassword) is not null;

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
        var staffId = user.FindFirst(AuthClaims.StaffMemberId) is not null ? user.GetStaffMemberId() : 0;
        return _tenant = new Tenant(user.GetOrganizationId(), staffId, user.GetUserId(), user.CanManage(), user.Identity?.Name ?? "", user.IsInRole(nameof(StaffRole.Owner)));
    }
}

public record Tenant(int OrganizationId, int StaffMemberId, int UserId, bool CanManage, string DisplayName, bool IsOwner = false);
