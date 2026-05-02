using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Services;
public class DatabaseExecutor(ILogger<DatabaseExecutor> Logger)
{
    internal static SqliteConnection GetNewDbConnection() =>new(Constants.DatabasePath);

    private  SqliteCommand CreateCommand(string? commandText)
    {
        var connection = GetNewDbConnection();
        try
        {
            var command = connection.CreateCommand();
            if (!string.IsNullOrWhiteSpace(commandText))
            {
                command.CommandText = commandText;
            }
            return command;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error dbExecution");
            throw;
        }
       
    }
      
    internal async Task<int> ExecuteAsync(string query, List<SqliteParameter>? parameters = null)
    {
        SqliteCommand? command = null;
        try
        {
            command = CreateCommand(query);
            parameters??= [];
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            if (command is not null)
            {
                await command.Connection!.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
            return -1;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error Executing");
            throw;
        }
        finally
        {            
            Disposer.Dispose(ref command);
        }
    }

    internal async Task<IReadOnlyList<T>> ReadDataAsync<T>(string query, List<SqliteParameter>? parameters = null)
        where T:class, new()
    {
        List<T> result = [];
        SqliteCommand? command = null;
        SqliteDataReader? reader = null;
        try
        {
            command = CreateCommand(query);
            parameters??= [];
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }
            await command.Connection!.OpenAsync();
            reader = await command.ExecuteReaderAsync();
            if (!reader.HasRows) return result;
            while (await reader.ReadAsync())
            {
                T item = new();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var property = typeof(T).GetProperty(reader.GetName(i));
                    if (property is not null && !await reader.IsDBNullAsync(i))
                    {
                        var value = reader.GetValue(i);
                        property.SetValue(item, value);
                    }
                }
                result.Add(item);
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error Reading data");
            throw;
        }
        finally
        {
            reader?.Dispose();
            Disposer.Dispose(ref command);
        }
    }

    internal async Task<T?> ReadSingleDataAsync<T>(string query, List<SqliteParameter>? parameters = null)
        where T : class, new()
    {
        var result = await ReadDataAsync<T>(query , parameters);
        if(result.Any())
        {
            return result[0];
        }
        return default;
    }
}
