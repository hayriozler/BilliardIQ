using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Components;
using Scoreboard.WebApp.Data;
using Scoreboard.WebApp.Endpoints;
using Scoreboard.WebApp.Middlewares;
using Scoreboard.WebApp.Security;
using Scoreboard.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

const string clientCorsPolicy = "Client";

builder.Services.AddOpenApi();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<Loc>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClubService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<PlayerService>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AssociationService>();
builder.Services.AddScoped<Scoreboard.WebApp.Services.Tournaments.CupService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ClientIdService>();
builder.Services.AddScoped<SystemPlayerService>();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<ScopedRunner>();

builder.Services.AddCors(options =>
    options.AddPolicy(clientCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ??
            ["https://localhost:7272", "http://localhost:5098"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "BillardIQAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            // API callers get a status code, browsers get the login page.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DataContext>().Database.MigrateAsync();
}

// Organizations created before client ids existed get one.
using (var backfillScope = app.Services.CreateScope())
{
    await backfillScope.ServiceProvider.GetRequiredService<ClientIdService>().EnsureAllAsync();
    await backfillScope.ServiceProvider.GetRequiredService<SystemPlayerService>().EnsureAllAsync();
}

// Optional seed accounts (Seed:Accounts): each salon + owner is created if its e-mail is not registered yet.
foreach (var account in app.Configuration.GetSection("Seed:Accounts").GetChildren())
{
    var email = account["Email"];
    var password = account["Password"];
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) continue;

    using var scope = app.Services.CreateScope();
    var normalized = email.Trim().ToLowerInvariant();
    if (await scope.ServiceProvider.GetRequiredService<DataContext>().UserSet.AnyAsync(u => u.Email == normalized)) continue;

    await scope.ServiceProvider.GetRequiredService<AuthService>().RegisterOrganizationAsync(
        account["OrganizationName"] ?? "Demo Salon", account["Name"] ?? "Admin", email, password, account["Language"]);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var cultures = Loc.Languages.Select(l => l.Code).ToArray();
app.UseRequestLocalization(options => options
    .SetDefaultCulture(Loc.DefaultLanguage)
    .AddSupportedCultures(cultures)
    .AddSupportedUICultures(cultures));

app.UseCors(clientCorsPolicy);

app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ClientIdMiddleware>();

app.MapClubsEndpoints();
app.MapPlayersEndpoints();
app.MapMatchStatsEndpoints();
app.MapTeamsEndpoints();
app.MapAuthEndpoints();

app.MapPost("/culture", (HttpContext context, [FromForm] string lang, [FromForm] string? returnUrl) =>
{
    if (Loc.Languages.Any(l => l.Code == lang))
    {
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/", SameSite = SameSiteMode.Lax });
    }

    var isLocal = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\");
    return Results.LocalRedirect(isLocal ? returnUrl! : "/");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
