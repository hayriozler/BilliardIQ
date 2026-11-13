namespace BilliardIQ.Mobile.Services;

// Rolling debug log of raw WS send/receive traffic (plus, from ScoreboardPageModel, matchResult
// validation/insert outcomes), written next to the SQLite DB so it can be pulled off-device the
// same way: adb exec-out run-as <package> cat files/ws-messages.log
// Capped at 5MB: once the file passes that size it's overwritten (truncated) rather than kept
// growing or rotated to a second file, so on-device storage use never exceeds 5MB.
internal static class WsTrafficLogger
{
    private const long MaxLogBytes = 5 * 1024 * 1024;
    private static readonly string _logPath = Path.Combine(FileSystem.AppDataDirectory, "ws-messages.log");

    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(string line)
    {
        try
        {
            if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaxLogBytes)
                File.Delete(_logPath);

            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort diagnostic logging — never let a logging failure break message handling.
        }
    }
}
