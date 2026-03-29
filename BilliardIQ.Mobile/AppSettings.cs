namespace BilliardIQ.Mobile;

public static class AppSettings
{
    /// <summary>
    /// Set to true to drop and recreate the database on next startup.
    /// Remember to set back to false after use.
    /// </summary>
    public static bool DropDatabaseOnStartup { get; set; } = false;
}
