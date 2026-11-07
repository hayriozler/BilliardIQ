using System.Text.Json;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scoreboard.Client.Components;
using Scoreboard.Client.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
var step = "creating the host builder";

try
{
    var builder = WebApplication.CreateBuilder(args);

    step = "loading appsettings.Local.json";
    if (IsReadableJson(Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json")))
    {
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    }

    step = "configuring logging";
    var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    TryCreateDirectory(logsFolder);

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

    step = "creating the database folder";
    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Db");
    Directory.CreateDirectory(folder);
    var dbName = Path.Combine(folder, "scoreboard.db3");

    step = "creating the player photos folder";
    TryCreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PlayerSet"));

    step = "registering services";
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents(options => options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30));

    builder.Services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dbName}"));
    builder.Services.AddScoped<LocalizationService>();
    builder.Services.AddSingleton<SystemPowerService>();
    builder.Services.AddSingleton<BoardSessionGuard>();
    builder.Services.AddSingleton<LanguageSync>();
    builder.Services.AddScoped<BoardSessionTracker>();
    builder.Services.AddScoped<CircuitHandler, BoardCircuitHandler>();
    builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(BindRemoteSyncOptions(builder.Configuration.GetSection("RemoteSync"))));
    builder.Services.AddSingleton<StartupGate>();
    builder.Services.AddTransient<TransientRetryHandler>();
    builder.Services.AddHttpClient(nameof(RemoteSyncService), c => c.Timeout = Timeout.InfiniteTimeSpan).AddHttpMessageHandler<TransientRetryHandler>();
    builder.Services.AddHostedService<RemoteSyncService>();
    builder.Services.AddSingleton<ServerClock>();
    builder.Services.AddTransient<ServerClockHandler>();
    builder.Services.AddHttpClient(nameof(RemotePullService), c => c.Timeout = Timeout.InfiniteTimeSpan).AddHttpMessageHandler<TransientRetryHandler>().AddHttpMessageHandler<ServerClockHandler>();
    builder.Services.AddHostedService<RemotePullService>();

    step = "building the application";
    var app = builder.Build();
    app.UseAntiforgery();

    step = "initializing the database";
    RunWithRetry(() => app.Services.InitializeDb(), attempts: 5, delay: TimeSpan.FromSeconds(2));

    step = "reading the time zone setting";
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var zoneId = db.SettingsSet.AsNoTracking().Where(s => s.Id == "TimeZone").Select(s => s.Value).FirstOrDefault();
        app.Services.GetRequiredService<ServerClock>().UseTimeZone(zoneId);
    }
    catch (SqliteException ex)
    {
        Log.Warning(ex, "Could not read the time zone setting; the system time zone is used for now");
    }

    step = "configuring the request pipeline";
    app.UseStaticFiles();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    step = "running the web host";
    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Startup failed while {Step}", step);
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

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

static bool IsReadableJson(string path)
{
    if (!File.Exists(path))
    {
        return true;
    }

    try
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        return true;
    }
    catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
    {
        Log.Error(ex, "{Path} cannot be read and is ignored; its settings are not applied", path);
        return false;
    }
}

static void TryCreateDirectory(string path)
{
    try
    {
        Directory.CreateDirectory(path);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Log.Error(ex, "Cannot create folder {Path} as user {User}; check its owner and permissions", path, Environment.UserName);
    }
}

static RemoteSyncOptions BindRemoteSyncOptions(IConfigurationSection section)
{
    try
    {
        var options = new RemoteSyncOptions();
        section.Bind(options);
        return options;
    }
    catch (InvalidOperationException ex)
    {
        Log.Error(ex, "The RemoteSync settings are invalid; remote sync is disabled for this run");
        return new RemoteSyncOptions();
    }
}

static void RunWithRetry(Action action, int attempts, TimeSpan delay)
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            action();
            return;
        }
        catch (Exception ex) when (attempt < attempts && ex is SqliteException or IOException)
        {
            Log.Warning(ex, "Database initialization failed (attempt {Attempt} of {Attempts}); retrying in {Delay} s", attempt, attempts, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }
}
