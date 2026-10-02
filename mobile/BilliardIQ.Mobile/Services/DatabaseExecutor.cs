using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
      
    internal async Task<bool> ExecuteAsync(string query, List<SqliteParameter>? parameters = null)
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
                return await command.ExecuteNonQueryAsync() >= 0;
            }
            return false;
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

            var customAttrDic = typeof(T).GetProperties()
            .Select(p => (p, attr: p.GetCustomAttribute<DbFieldNameAttribute>()))
            .Where(x => x.attr != null)
            .ToDictionary(
                x => x.attr!.FieldName,
                x => x.p.Name
            );
            while (await reader.ReadAsync())
            {
                T item = new();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    PropertyInfo? propertyInfo = null;
                    if (customAttrDic.TryGetValue(fieldName, out string? propertyName))
                        propertyInfo = typeof(T).GetProperty(propertyName);
                    else propertyInfo = typeof(T).GetProperty(fieldName);
                    if (propertyInfo is not null && !await reader.IsDBNullAsync(i))
                    {
                        var value = reader.GetValue(i);
                        if((propertyInfo.PropertyType == typeof(Int32) || propertyInfo.PropertyType.IsEnum))
                        {
                            propertyInfo.SetValue(item, Convert.ToInt32(value));
                        }
                        else if (propertyInfo.PropertyType == typeof(DateTime))
                        {
                            propertyInfo.SetValue(item, Convert.ToDateTime(value));
                        }
                        else if (propertyInfo.PropertyType == typeof(bool))
                        {
                            propertyInfo.SetValue(item, Convert.ToBoolean(value));
                        }
                        else
                        {
                            propertyInfo.SetValue(item, value);
                        }
                        
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
