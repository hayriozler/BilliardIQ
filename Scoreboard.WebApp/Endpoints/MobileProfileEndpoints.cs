using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public record MobileOwnProfileRequest(
    string? Nickname, int? AvatarId, string? PhotoBase64, bool RemovePhoto,
    Level? Level, string? LicenseNo, DateOnly? LicenseValidUntil, DateOnly? BirthDate, Gender? Gender, Handedness? Handedness,
    string? Phone, string? Locale);

public record MobileManagedProfileRequest(
    string? FirstName, string? LastName, string? Nickname, int? AvatarId, string? PhotoBase64, bool RemovePhoto,
    Level? Level, string? LicenseNo, DateOnly? LicenseValidUntil, DateOnly? BirthDate, Gender? Gender, Handedness? Handedness,
    string? Phone, string? Locale);

public static class MobileProfileEndpoints
{
    public static IEndpointRouteBuilder MapMobileProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var own = app.MapGroup("/api/mobile/player/profile")
            .WithTags("MobileProfile")
            .RequireAuthorization(JwtSettings.MobilePlayerPolicy);

        own.MapGet("/", async (HttpContext context, PlayerProfileService profiles) =>
            await profiles.GetAsync(context.User.GetPlayerId()) is { } profile ? Results.Ok(profile) : Results.NotFound());

        own.MapPut("/", async (MobileOwnProfileRequest request, HttpContext context, PlayerProfileService profiles, Loc loc) =>
        {
            try
            {
                var update = new PlayerDetailsUpdate(
                    null, null, request.Nickname, request.AvatarId, request.PhotoBase64, request.RemovePhoto,
                    request.Level, request.LicenseNo, request.LicenseValidUntil, request.BirthDate, request.Gender, request.Handedness,
                    request.Phone, request.Locale);
                return Results.Ok(await profiles.UpdateAsync(context.User.GetPlayerId(), update, canChangeName: false));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        var manage = app.MapGroup("/api/mobile/manage/players")
            .WithTags("MobileProfile")
            .RequireAuthorization(JwtSettings.MobileManagerPolicy);

        manage.MapGet("/{id:int}/profile", async (int id, PlayerProfileService profiles) =>
            await profiles.GetAsync(id) is { } profile ? Results.Ok(profile) : Results.NotFound());

        manage.MapPut("/{id:int}/profile", async (int id, MobileManagedProfileRequest request, PlayerProfileService profiles, Loc loc) =>
        {
            try
            {
                var update = new PlayerDetailsUpdate(
                    request.FirstName, request.LastName, request.Nickname, request.AvatarId, request.PhotoBase64, request.RemovePhoto,
                    request.Level, request.LicenseNo, request.LicenseValidUntil, request.BirthDate, request.Gender, request.Handedness,
                    request.Phone, request.Locale);
                return Results.Ok(await profiles.UpdateAsync(id, update, canChangeName: true));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(loc.Error(ex));
            }
        });

        var avatars = app.MapGroup("/api/mobile/avatars").WithTags("MobileProfile");

        avatars.MapGet("/count", () => Results.Ok(new { count = Scoreboard.Common.AvatarGenerator.Count }))
            .RequireAuthorization(JwtSettings.MobileAnyPolicy);

        return app;
    }
}
