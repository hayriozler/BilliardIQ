using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Scoreboard.Client.Services;

public partial class SystemPowerService(IDbContextFactory<DataContext> dbFactory, ILogger<SystemPowerService> logger)
{
    public const int HoldMilliseconds = 4000;

    private readonly HashSet<string> _heldKeys = [];
    private CancellationTokenSource? _holdCts;

    public bool IsShuttingDown { get; private set; }

    public bool IsHeld(string key) => _heldKeys.Contains(key);

    public void NotifyKeyDown(string key)
    {
        if (IsShuttingDown || !_heldKeys.Add(key))
        {
            return;
        }

        if (IsHeld("NumLock") && IsHeld("Insert"))
        {
            StartHold(RebootAsync);
        }
        else if (IsHeld("NumLock") && IsHeld("Delete"))
        {
            StartHold(ShutdownAsync);
        }
    }

    public void NotifyKeyUp(string key)
    {
        _heldKeys.Remove(key);
        _holdCts?.Cancel();
    }

    private void StartHold(Func<Task> action)
    {
        _holdCts?.Cancel();
        var cts = new CancellationTokenSource();
        _holdCts = cts;
        _ = RunHoldAsync(cts, action);
    }

    private static async Task RunHoldAsync(CancellationTokenSource cts, Func<Task> action)
    {
        try
        {
            await Task.Delay(HoldMilliseconds, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await action();
    }

    public Task RebootAsync() => ExecutePowerActionAsync("reboot", "reboot");

    public Task ShutdownAsync() => ExecutePowerActionAsync("shutdown", "poweroff");

    private async Task ExecutePowerActionAsync(string label, string systemctlVerb)
    {
        if (IsShuttingDown)
        {
            return;
        }
        IsShuttingDown = true;

        LogPowerActionTriggered(label);

        await CheckpointDatabaseAsync(label);

        if (!OperatingSystem.IsLinux() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            LogPowerActionSkipped(label);
            return;
        }

        Process.Start(new ProcessStartInfo("sudo", ["systemctl", systemctlVerb]) { UseShellExecute = false });
    }

    private async Task CheckpointDatabaseAsync(string label)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            await db.Database.CloseConnectionAsync();
        }
        catch (Exception ex)
        {
            LogCheckpointFailed(ex, label);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "System {Action} triggered from keyboard hold combo.")]
    private partial void LogPowerActionTriggered(string action);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping actual system {Action} - not running on Linux.")]
    private partial void LogPowerActionSkipped(string action);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Database checkpoint before system {Action} failed.")]
    private partial void LogCheckpointFailed(Exception ex, string action);
}
