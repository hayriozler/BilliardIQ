using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Zeymera.BillardIQ.WebApp.Components;
using Zeymera.BillardIQ.WebApp.Data;
using Zeymera.BillardIQ.WebApp.Endpoints;
using Zeymera.BillardIQ.WebApp.Middlewares;
using Zeymera.BillardIQ.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

const string clientCorsPolicy = "Client";

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ScoreboardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ClubService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<PlayerService>();

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
        options.Cookie.Name = "ZeymeraClubAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(clientCorsPolicy);

app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ClientIdMiddleware>();

app.MapClubsEndpoints();
app.MapScoreboardClientsEndpoints();
app.MapPlayersEndpoints();
app.MapMatchStatsEndpoints();
app.MapTeamsEndpoints();
app.MapClubAuthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
