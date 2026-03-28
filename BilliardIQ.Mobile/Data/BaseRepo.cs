using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public abstract class BaseRepo(ILogger<BaseRepo> Logger)
{
    internal async Task InitAsync(string sql)
    {
        SqliteCommand? cmd = null;
        try
        {
            await DatabaseService.InitializeTableAsync(sql);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Player table");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    internal async Task<SqliteDataReader> GetSqliteReaderAsync(string tableCreationSql, string sql, List<SqliteParameter> parameters)
    {
        await InitAsync(tableCreationSql);
        SqliteCommand? cmd = await DatabaseService.GetCommandAsync(sql);
        foreach (var parameter in parameters)
        {
            cmd.Parameters.Add(parameter);
        }
        return await cmd.ExecuteReaderAsync();
    }
    internal async Task<SqliteCommand> GetSqliteCommandAsync(string tableCreationSql, string sql)
    {
        await InitAsync(tableCreationSql);
        return await DatabaseService.GetCommandAsync(sql);
    }
}
