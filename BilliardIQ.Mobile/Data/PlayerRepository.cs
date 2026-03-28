using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public class PlayerRepository(ILogger<PlayerRepository> Logger) : BaseRepo(Logger)
{    
    private readonly string _tableCreationSql = @"
             CREATE TABLE IF NOT EXISTS Player (
                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                 Name TEXT NOT NULL,
                 LastName TEXT NOT NULL,
                 Level INTEGER NOT NULL,
                 CreatedAt DateTime NOT NULL
             );";
    
    private async Task<SqliteCommand> GetSqliteCommandAsync(bool isPlayExists, Player player) {
        SqliteCommand cmd;
        if (!isPlayExists)
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"INSERT INTO Player(Name, LastName, Level, CreatedAt) VALUES (@Name, @LastName, @Level, @CreatedAt)");
            cmd.Parameters.AddWithValue("@Name", player.Name);
            cmd.Parameters.AddWithValue("@LastName", player.LastName);
            cmd.Parameters.AddWithValue("@Level", player.Level);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        }
        else
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"UPDATE Player SET Name = @Name, LastName =@LastName, Level = @Level");
            cmd.Parameters.AddWithValue("@Name", player.Name);
            cmd.Parameters.AddWithValue("@LastName", player.LastName);
            cmd.Parameters.AddWithValue("@Level", player.Level);
        }
        return cmd;
    }
    public async Task UpsertAsync(Player player)
    {
        SqliteCommand? cmd = null;
        try
        {
            var currentDbPlayer = await GetPlayerAsync();
            bool isPlayerExists = currentDbPlayer is not null;
            cmd = await GetSqliteCommandAsync(isPlayerExists, player);
            var result = await cmd.ExecuteNonQueryAsync();
            if (result > 0)
            {
                if (!isPlayerExists)
                {
                    Logger.LogInformation("Player has been created successfully");
                }
                else
                {
                    Logger.LogInformation("Player has been updated successfully");
                }
            }
            else
            {
                Logger.LogDebug("Something went wrong....");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    public async Task<Player?> GetPlayerAsync()
    {
        SqliteCommand? cmd = null;
        try
        {
            using var reader = await GetSqliteReaderAsync(_tableCreationSql, "select * from Player", Enumerable.Empty<SqliteParameter>().ToList());
            if (!await reader.ReadAsync() || !reader.HasRows)
            {
                return null;
            }
            var idxLevel = reader.GetOrdinal("Level");
            var idxName = reader.GetOrdinal("Name");
            var idxLastName = reader.GetOrdinal("LastName");
            var idxCreatedAt = reader.GetOrdinal("CreatedAt");
            return new Player
            {
                Name = reader.GetString(idxName),
                LastName = reader.GetString(idxLastName),
                Level = (Level)reader.GetInt32(idxLevel),
                CreatedAt = reader.GetDateTime(idxCreatedAt)
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }
}
