using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Security;

public class SessionRevalidator(IServiceScopeFactory scopes, ILogger<SessionRevalidator> logger)
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private const int _trimThreshold = 1000;

    private readonly ConcurrentDictionary<(int UserId, int StaffId, int PlayerId), Entry> _entries = new();

    private sealed record State(bool Active, string Stamp);

    private sealed record Entry(State State, DateTimeOffset ExpiresAt);

    public async Task<bool> IsValidAsync(ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return false;
        }

        _ = int.TryParse(principal.FindFirstValue(AuthClaims.StaffMemberId), out var staffId);
        _ = int.TryParse(principal.FindFirstValue(MobileClaims.PlayerId), out var playerId);
        var key = (userId, staffId, playerId);
        var now = DateTimeOffset.UtcNow;
        if (!_entries.TryGetValue(key, out var entry) || entry.ExpiresAt <= now)
        {
            State? loaded;
            try
            {
                loaded = await LoadAsync(userId, staffId, playerId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Session revalidation failed; keeping the current session.");
                return true;
            }

            loaded ??= new State(false, "");
            entry = new Entry(loaded, now + Interval);
            if (_entries.Count > _trimThreshold)
            {
                Trim(now);
            }

            _entries[key] = entry;
        }

        return entry.State.Active && entry.State.Stamp == principal.FindFirstValue(AuthClaims.SecurityStamp);
    }

    public void Invalidate(int userId)
    {
        foreach (var key in _entries.Keys.Where(k => k.UserId == userId))
        {
            _entries.TryRemove(key, out _);
        }
    }

    private void Trim(DateTimeOffset now)
    {
        foreach (var pair in _entries.Where(p => p.Value.ExpiresAt <= now))
        {
            _entries.TryRemove(pair.Key, out _);
        }
    }

    private async Task<State?> LoadAsync(int userId, int staffId, int playerId)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        return await db.UserSet.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new State(
                u.Status == UserStatus.Active &&
                (u.OrganizationId == null ||
                 (playerId > 0 && staffId == 0 && db.PlayerSet.IgnoreQueryFilters().Any(p => p.Id == playerId && p.UserId == userId && p.DeletedAt == null && !p.IsSystem && p.CreatedInOrganizationId == u.OrganizationId &&
                     db.OrganizationSet.Any(o => o.Id == p.CreatedInOrganizationId && o.IsActive && o.DeletedAt == null))) ||
                 db.StaffMemberSet.Any(s => s.Id == staffId && s.UserId == userId && s.IsActive && s.DeletedAt == null && s.Organization.IsActive)),
                u.SecurityStamp))
            .FirstOrDefaultAsync();
    }
}

public static class CookieSessionValidation
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        var revalidator = context.HttpContext.RequestServices.GetRequiredService<SessionRevalidator>();
        if (!await revalidator.IsValidAsync(context.Principal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}

public class RevalidatingAuthenticationStateProvider(ILoggerFactory loggerFactory, IServiceScopeFactory scopes)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => SessionRevalidator.Interval;

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            return true;
        }

        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SessionRevalidator>().IsValidAsync(authenticationState.User);
    }
}
