using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
            HttpContext context, AuthService auth,
            [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl) =>
        {
            var result = await auth.LoginAsync(email, password);
            if (result is null)
            {
                return Results.Redirect($"/login?error=1&email={Uri.EscapeDataString(email ?? "")}");
            }

            await SignInAsync(context, result);
            return Results.LocalRedirect(IsLocal(returnUrl) ? returnUrl! : "/");
        }).DisableAntiforgery();

        app.MapPost("/register", async (
            HttpContext context, AuthService auth, Loc loc,
            [FromForm] string organization, [FromForm] string name, [FromForm] string email,
            [FromForm] string password, [FromForm] string? language) =>
        {
            try
            {
                var result = await auth.RegisterOrganizationAsync(organization, name, email, password, language);
                await SignInAsync(context, result);
                return Results.LocalRedirect("/");
            }
            catch (ArgumentException ex)
            {
                var query = $"error={Uri.EscapeDataString(loc.Error(ex))}" +
                            $"&organization={Uri.EscapeDataString(organization ?? "")}" +
                            $"&name={Uri.EscapeDataString(name ?? "")}" +
                            $"&email={Uri.EscapeDataString(email ?? "")}" +
                            $"&language={Uri.EscapeDataString(language ?? "")}";
                return Results.Redirect($"/register?{query}");
            }
        }).DisableAntiforgery();

        app.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).DisableAntiforgery();

        return app;
    }

    private static Task SignInAsync(HttpContext context, LoginResult result) =>
        context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthClaims.CreatePrincipal(result.User, result.Staff, result.Organization, CookieAuthenticationDefaults.AuthenticationScheme),
            new AuthenticationProperties { IsPersistent = true });

    private static bool IsLocal(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
