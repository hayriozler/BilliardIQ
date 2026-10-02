using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Security;

public static class JwtSessionValidation
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
            !int.TryParse(principal.FindFirstValue(MobileClaims.RefreshTokenId), out var tokenId))
        {
            context.Fail("Invalid session.");
            return;
        }

        using var scope = context.HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var now = DateTimeOffset.UtcNow;
        var active = await db.RefreshTokenSet.AsNoTracking().AnyAsync(t =>
            t.Id == tokenId && t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now && t.User.Status == UserStatus.Active);
        if (!active)
        {
            context.Fail("Session is no longer valid.");
        }
    }
}
