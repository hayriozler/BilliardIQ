using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class ClubAuthEndpoints
{
    public const string ClubIdClaimType = "ClubId";

    public static IEndpointRouteBuilder MapClubAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (HttpContext context, ClubService clubs, [FromForm] string code) =>
        {
            var club = await clubs.FindByCodeAsync(code.Trim());
            if (club is null)
            {
                return Results.Redirect("/login?error=1");
            }

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, club.Id.ToString()),
                new Claim(ClaimTypes.Name, club.Name),
                new Claim(ClubIdClaimType, club.Id.ToString())
            ], CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Results.Redirect("/players");
        }).DisableAntiforgery();

        app.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).DisableAntiforgery();

        return app;
    }
}
