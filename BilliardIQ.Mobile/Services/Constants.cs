namespace BilliardIQ.Mobile.Services;

internal static class Constants
{
    internal const string DatabaseFileName = "BillardIQ.db3";    
    internal static string DatabasePath => $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName)}";
}
