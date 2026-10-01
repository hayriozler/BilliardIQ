using Microsoft.AspNetCore.Authentication;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

namespace Scoreboard.WebApp.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
            HttpContext context, AuthService auth, IDataProtectionProvider dataProtection,
            [FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl) =>
        {
            var user = await auth.VerifyAsync(email, password);
            var memberships = user is null ? [] : await auth.MembershipsAsync(user.Id);
            if (user is null || memberships.Count == 0)
            {
                return Results.Redirect($"/login?error=1&email={Uri.EscapeDataString(email ?? "")}");
            }

            if (memberships.Count > 1)
            {
                context.Response.Cookies.Append(PickCookie, PickProtector(dataProtection).Protect(user.Id.ToString(), TimeSpan.FromMinutes(10)),
                    new CookieOptions { HttpOnly = true, IsEssential = true, Path = "/", SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromMinutes(10) });
                return Results.Redirect("/login/organization" + (IsLocal(returnUrl) ? $"?returnUrl={Uri.EscapeDataString(returnUrl!)}" : ""));
            }

            var result = await auth.LoginToAsync(user.Id, memberships[0].OrganizationId);
            await SignInAsync(context, result!);
            return Results.LocalRedirect(IsLocal(returnUrl) ? returnUrl! : "/");
        }).DisableAntiforgery();

        app.MapGet("/login/organization", async (HttpContext context, AuthService auth, Loc loc, IDataProtectionProvider dataProtection, string? returnUrl) =>
        {
            if (PickedUserId(context, dataProtection) is not { } userId)
            {
                return Results.Redirect("/login");
            }

            var memberships = await auth.MembershipsAsync(userId);
            var items = string.Concat(memberships.Select(m =>
                $"<button type=\"submit\" name=\"organizationId\" value=\"{m.OrganizationId}\" class=\"primary\" style=\"display:block;width:100%;margin-bottom:.6rem\">{WebUtility.HtmlEncode(m.Organization.Name)}</button>"));
            var html = $$"""
                <!DOCTYPE html><html lang="{{Loc.Current}}"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{{WebUtility.HtmlEncode(loc["Salon seçin"])}} • BillardIQ</title><link rel="stylesheet" href="/app.css" /><link rel="icon" type="image/png" href="/images/zeymera-ram.png" /></head>
                <body><div class="auth-card">
                <div class="brand-row"><img class="brand-logo" src="/images/zeymera-ram.png" alt="Zeymera" /><span>BillardIQ</span></div>
                <h1>{{WebUtility.HtmlEncode(loc["Salon seçin"])}}</h1>
                <p class="muted">{{WebUtility.HtmlEncode(loc["Hangi salona giriş yapmak istiyorsunuz?"])}}</p>
                <form method="post" action="/login/organization"><input type="hidden" name="returnUrl" value="{{WebUtility.HtmlEncode(IsLocal(returnUrl) ? returnUrl : "")}}" />{{items}}</form>
                <div class="switch"><a href="/login">{{WebUtility.HtmlEncode(loc["Başka bir hesapla giriş yap"])}}</a></div>
                </div></body></html>
                """;
            return Results.Content(html, "text/html; charset=utf-8");
        });

        app.MapPost("/login/organization", async (
            HttpContext context, AuthService auth, IDataProtectionProvider dataProtection,
            [FromForm] int organizationId, [FromForm] string? returnUrl) =>
        {
            if (PickedUserId(context, dataProtection) is not { } userId)
            {
                return Results.Redirect("/login");
            }

            var result = await auth.LoginToAsync(userId, organizationId);
            if (result is null || result.Organization.Id != organizationId)
            {
                return Results.Redirect("/login/organization");
            }

            context.Response.Cookies.Delete(PickCookie);
            await SignInAsync(context, result);
            return Results.LocalRedirect(IsLocal(returnUrl) ? returnUrl! : "/");
        }).DisableAntiforgery();

        app.MapPost("/register", async (
            HttpContext context, AuthService auth, Loc loc,
            [FromForm] string organization, [FromForm] string name, [FromForm] string email,
            [FromForm] string password, [FromForm] string? country, [FromForm] string? currency, [FromForm] string? language) =>
        {
            try
            {
                var result = await auth.RegisterOrganizationAsync(organization, name, email, password, country, currency, language);
                await SignInAsync(context, result);
                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(result.Organization.Language)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/", SameSite = SameSiteMode.Lax });
                return Results.LocalRedirect("/");
            }
            catch (ArgumentException ex)
            {
                var query = $"error={Uri.EscapeDataString(loc.Error(ex))}" +
                            $"&organization={Uri.EscapeDataString(organization ?? "")}" +
                            $"&name={Uri.EscapeDataString(name ?? "")}" +
                            $"&email={Uri.EscapeDataString(email ?? "")}" +
                            $"&country={Uri.EscapeDataString(country ?? "")}" +
                            $"&currency={Uri.EscapeDataString(currency ?? "")}" +
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

    private const string PickCookie = "bq_pick";

    private static ITimeLimitedDataProtector PickProtector(IDataProtectionProvider provider) =>
        provider.CreateProtector("BillardIQ.LoginOrganizationPicker").ToTimeLimitedDataProtector();

    private static int? PickedUserId(HttpContext context, IDataProtectionProvider provider)
    {
        try
        {
            return context.Request.Cookies[PickCookie] is { Length: > 0 } value && int.TryParse(PickProtector(provider).Unprotect(value), out var id) ? id : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    private static Task SignInAsync(HttpContext context, LoginResult result) =>
        context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthClaims.CreatePrincipal(result.User, result.Staff, result.Organization, CookieAuthenticationDefaults.AuthenticationScheme),
            new AuthenticationProperties { IsPersistent = true });

    private static bool IsLocal(string? url) =>
        !string.IsNullOrEmpty(url) && url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
}
