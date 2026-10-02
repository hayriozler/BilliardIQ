using Microsoft.AspNetCore.Mvc;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record MobileLoginRequest(string Email, string Password, string? DeviceName);
public record MobileRefreshRequest(string RefreshToken);
public record MobileOrganizationRequest(int OrganizationId);
public record MobilePasswordRequest(string CurrentPassword, string NewPassword);
public record MobileInviteRegisterRequest(string Code, string Email, string Password, string? DeviceName);
public record MobileProfileRequest(string? DisplayName, string? Locale, string? Phone);
public record MobileEmailRequest(string Email, string Password);
public record MobilePlayerAccountRequest(string Email);
public record MobilePlayerProfileRequest(string Name, string? Nickname, int? AvatarId, string? PhotoBase64, string? PhotoExtension);

public record MobilePlayerProfile(int Id, string Name, string? Nickname, string DisplayName, int? AvatarId, string? PhotoUrl, Level Level, int? ShortcutNumber);

public record MobileMatch(
    int MatchId, DateTimeOffset PlayedAt, string OpponentName, int PlayerScore, int OpponentScore,
    int Inning, int HighRun, double Average, bool Won);

public record MobilePlayerHome(MobilePlayerProfile Player, StatSummary Summary, IReadOnlyList<MobileMatch> LastMatches);

public static class MobileEndpoints
{
    public static IEndpointRouteBuilder MapMobileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile").WithTags("Mobile");

        var auth = group.MapGroup("/auth");

        auth.MapPost("/login", async (MobileLoginRequest request, MobileAuthService mobile) =>
            await mobile.LoginAsync(request.Email, request.Password, request.DeviceName) is { } session
                ? Results.Ok(session)
                : Results.Unauthorized());

        auth.MapPost("/refresh", async (MobileRefreshRequest request, MobileAuthService mobile) =>
            await mobile.RefreshAsync(request.RefreshToken) is { } session
                ? Results.Ok(session)
                : Results.Unauthorized());

        auth.MapPost("/logout", async (MobileRefreshRequest request, MobileAuthService mobile) =>
        {
            await mobile.LogoutAsync(request.RefreshToken);
            return Results.NoContent();
        });

        auth.MapPost("/register-player", async (MobileInviteRegisterRequest request, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                return Results.Ok(await mobile.RegisterWithInviteAsync(request.Code, request.Email, request.Password, request.DeviceName));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        auth.MapPost("/organization", async (MobileOrganizationRequest request, HttpContext context, MobileAuthService mobile) =>
            await mobile.SelectOrganizationAsync(context.User.GetUserId(), context.User.GetRefreshTokenId(), request.OrganizationId) is { } session
                ? Results.Ok(session)
                : Results.Forbid())
            .RequireAuthorization(JwtSettings.MobileAnyPolicy);

        auth.MapPost("/password", async (MobilePasswordRequest request, HttpContext context, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                return Results.Ok(await mobile.ChangePasswordAsync(context.User.GetUserId(), context.User.GetRefreshTokenId(), request.CurrentPassword, request.NewPassword));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        }).RequireAuthorization(JwtSettings.MobileAnyPolicy);

        var me = group.MapGroup("/me");

        me.MapGet("/", async (HttpContext context, MobileAuthService mobile) =>
            await mobile.GetSessionAsync(context.User.GetUserId(), context.User.GetRefreshTokenId()) is { } session
                ? Results.Ok(session)
                : Results.Unauthorized())
            .RequireAuthorization(JwtSettings.MobileAnyPolicy);

        me.MapPut("/profile", async (MobileProfileRequest request, HttpContext context, MobileAuthService mobile) =>
        {
            await mobile.UpdateProfileAsync(context.User.GetUserId(), request.DisplayName, request.Locale, request.Phone);
            return Results.NoContent();
        }).RequireAuthorization(JwtSettings.MobilePolicy);

        me.MapPut("/email", async (MobileEmailRequest request, HttpContext context, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                await mobile.UpdateEmailAsync(context.User.GetUserId(), request.Email, request.Password);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        }).RequireAuthorization(JwtSettings.MobilePolicy);

        var player = group.MapGroup("/player").RequireAuthorization(JwtSettings.MobilePlayerPolicy);

        player.MapGet("/home", async (HttpContext context, StatsService stats) =>
        {
            var detail = await stats.PlayerAsync(context.User.GetPlayerId());
            if (detail is null)
            {
                return Results.NotFound();
            }

            var last = detail.Matches
                .OrderByDescending(m => m.PlayedAt)
                .Take(5)
                .Select(m => new MobileMatch(m.MatchId, m.PlayedAt, m.OpponentName, m.PlayerScore, m.OpponentScore, m.Inning, m.HighRun, m.Average, m.Won))
                .ToList();
            return Results.Ok(new MobilePlayerHome(ToProfile(detail.Player), detail.Summary, last));
        });

        player.MapGet("/profile", async (HttpContext context, PlayerService players) =>
            await players.GetAsync(context.User.GetPlayerId()) is { } p ? Results.Ok(ToProfile(p)) : Results.NotFound());

        player.MapPut("/profile", async (MobilePlayerProfileRequest request, HttpContext context, PlayerService players, Loc loc) =>
        {
            try
            {
                var updated = await players.UpdateOwnProfileAsync(context.User.GetPlayerId(), request.Name, request.Nickname, request.AvatarId, request.PhotoBase64, request.PhotoExtension);
                return Results.Ok(ToProfile(updated));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        var manage = group.MapGroup("/players").RequireAuthorization(JwtSettings.MobileManagerPolicy);

        manage.MapPost("/{id:int}/account", async (int id, MobilePlayerAccountRequest request, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                return Results.Ok(await mobile.CreatePlayerAccountAsync(id, request.Email));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        manage.MapPost("/{id:int}/account/reset", async (int id, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                return Results.Ok(await mobile.ResetPlayerPasswordAsync(id));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        manage.MapPost("/{id:int}/invite", async (int id, MobileAuthService mobile, Loc loc) =>
        {
            try
            {
                return Results.Ok(await mobile.CreateInviteAsync(id));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        return app;
    }

    private static MobilePlayerProfile ToProfile(Player p) =>
        new(p.Id, $"{p.FirstName} {p.LastName}".Trim(), p.Nickname, p.DisplayName, p.AvatarId, p.PhotoUrl, p.Level, p.ShortcutNumber);
}
