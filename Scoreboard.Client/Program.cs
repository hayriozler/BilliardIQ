using Microsoft.EntityFrameworkCore;
using Scoreboard.Client.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Scoreboard.Client.Services;
using Serilog;
using Serilog.Events;

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
    .AddInteractiveServerComponents(options => options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));

string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Db");
if (!Directory.Exists(folder))
    Directory.CreateDirectory(folder);
var dbName = Path.Combine(folder, "scoreboard.db3");

string playerPhotosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PlayerSet");
if (!Directory.Exists(playerPhotosFolder))
    Directory.CreateDirectory(playerPhotosFolder);
builder.Services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dbName}"));
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddSingleton<SystemPowerService>();
builder.Services.AddSingleton<BoardSessionGuard>();
builder.Services.AddSingleton<LanguageSync>();
builder.Services.AddScoped<BoardSessionTracker>();
builder.Services.AddScoped<CircuitHandler, BoardCircuitHandler>();
builder.Services.Configure<RemoteSyncOptions>(builder.Configuration.GetSection("RemoteSync"));
builder.Services.AddHttpClient(nameof(RemoteSyncService), c => c.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHostedService<RemoteSyncService>();
builder.Services.AddSingleton<ServerClock>();
builder.Services.AddTransient<ServerClockHandler>();
builder.Services.AddHttpClient(nameof(RemotePullService), c => c.Timeout = TimeSpan.FromSeconds(30)).AddHttpMessageHandler<ServerClockHandler>();
builder.Services.AddHostedService<RemotePullService>();

var app = builder.Build();
app.UseAntiforgery();
app.Services.InitializeDbAsync();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
