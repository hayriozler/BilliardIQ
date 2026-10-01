using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Utilities;

internal static class Disposer
{
    internal static void Dispose(ref SqliteCommand? cmd)
    {
        cmd?.Connection?.Dispose();
        cmd?.Dispose();
    }
}
