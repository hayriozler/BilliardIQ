using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Zeymera.Scoreboard.Client.Components;
using Zeymera.Scoreboard.Client.Endpoints;
using Zeymera.Scoreboard.Client.Services;

var builder = WebApplication.CreateBuilder(args);

var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsFolder);

var logLevelSection = builder.Configuration.GetSection("Logging:LogLevel");
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(ParseLogLevel(logLevelSection["Default"]))
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsFolder, "scoreboard-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}");

foreach (var category in logLevelSection.GetChildren())
{
    if (category.Key != "Default")
    {
        loggerConfig = loggerConfig.MinimumLevel.Override(category.Key, ParseLogLevel(category.Value));
    }
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

static LogEventLevel ParseLogLevel(string? value) => value?.ToLowerInvariant() switch
{
    "trace" => LogEventLevel.Verbose,
    "debug" => LogEventLevel.Debug,
    "warning" => LogEventLevel.Warning,
    "error" => LogEventLevel.Error,
    "critical" => LogEventLevel.Fatal,
    "none" => LogEventLevel.Fatal + 1,
    _ => LogEventLevel.Information,
};

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Db");
if (!Directory.Exists(folder))
    Directory.CreateDirectory(folder);
var dbName = Path.Combine(folder, "scoreboard.db3");

string playerPhotosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PlayerSet");
if (!Directory.Exists(playerPhotosFolder))
    Directory.CreateDirectory(playerPhotosFolder);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7153/";
builder.Services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dbName}"));
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<ScoreboardCommandHub>();
builder.Services.AddSingleton<SystemPowerService>();
builder.Services.AddSingleton<BoardSessionGuard>();
builder.Services.Configure<RemoteSyncOptions>(builder.Configuration.GetSection("RemoteSync"));
builder.Services.AddHttpClient(nameof(RemoteSyncService));
builder.Services.AddHostedService<RemoteSyncService>();
builder.Services.AddHttpClient(nameof(RemotePullService));
builder.Services.AddHostedService<RemotePullService>();
builder.Services.AddHostedService<RemoteWsPushService>();

var app = builder.Build();
app.UseAntiforgery();
app.Services.InitializeDbAsync();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseWebSockets();
app.MapScoreboardWebSocket();

app.Run();
