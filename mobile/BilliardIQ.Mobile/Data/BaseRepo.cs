using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Data;

public abstract class BaseRepo
{
    internal static bool IsDbNull(object value) => value is DBNull || value == null;
    internal static bool IsDbNull(SqliteDataReader reader, string column)
    {
        try
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx);
        }
        catch { return true; }
    }
}
