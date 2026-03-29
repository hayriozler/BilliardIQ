using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;

namespace BilliardIQ.Mobile.Services;

internal static class DatabaseService
{
    private static SqliteConnection GetNewDbConnection()
        => new(Constants.DatabasePath);

    internal static Task<SqliteCommand> GetCommandAsync()
        => CreateCommandAsync(null);

    internal static Task<SqliteCommand> GetCommandAsync(string commandText)
        => CreateCommandAsync(commandText);

    private static async Task<SqliteCommand> CreateCommandAsync(string? commandText)
    {
        var connection = GetNewDbConnection();
        await connection.OpenAsync();

        var command = connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(commandText))
        {
            command.CommandText = commandText;
        }

        return command;
    }

    internal static async Task InitializeTableAsync(string Sql)
    {        
        SqliteCommand? cmd = null;
        try
        {
            cmd = await GetCommandAsync(Sql);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    internal static async Task InitializeDatabaseAsync()
    {
        
        var connection = GetNewDbConnection();
        await connection.OpenAsync();
        await connection.CloseAsync();
        await connection.DisposeAsync();

    }
    internal static async Task DropDatabaseAsync()
    {
        try
        {
            var fileName = $"{FileSystem.AppDataDirectory}/{Constants.DatabaseFileName}";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                Console.WriteLine("Old database deleted.");
            }

        }
        catch (Exception)
        {
            throw;
        }        
    }

}
