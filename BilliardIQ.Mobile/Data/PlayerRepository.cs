using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
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
                 Country TEXT NOT NULL DEFAULT '',
                 City TEXT NOT NULL DEFAULT '',
                 Club TEXT,
                 Association TEXT,
                 Email TEXT NOT NULL DEFAULT '',
                 Phone TEXT,
                 CreatedAt DateTime NOT NULL
             );";

    private readonly string[] _migrations =
    [
        "ALTER TABLE Player ADD COLUMN Country TEXT NOT NULL DEFAULT ''",
        "ALTER TABLE Player ADD COLUMN City TEXT NOT NULL DEFAULT ''",
        "ALTER TABLE Player ADD COLUMN Club TEXT",
        "ALTER TABLE Player ADD COLUMN Association TEXT",
        "ALTER TABLE Player ADD COLUMN Email TEXT NOT NULL DEFAULT ''",
        "ALTER TABLE Player ADD COLUMN Phone TEXT",
    ];

    private bool _migrationsDone = false;

    private async Task EnsureMigratedAsync()
    {
        if (_migrationsDone) return;
        await InitAsync(_tableCreationSql);
        foreach (var migration in _migrations)
        {
            SqliteCommand? cmd = null;
            try
            {
                cmd = await DatabaseService.GetCommandAsync(migration);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* column already exists */ }
            finally { Disposer.Dispose(ref cmd); }
        }
        _migrationsDone = true;
    }

    private async Task<SqliteCommand> GetPlayerSqliteCommandAsync(bool isPlayExists, Player player) {
        await EnsureMigratedAsync();
        SqliteCommand cmd;
        if (!isPlayExists)
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"INSERT INTO Player(Name, LastName, Level, Country, City, Club, Association, Email, Phone, CreatedAt) VALUES (@Name, @LastName, @Level, @Country, @City, @Club, @Association, @Email, @Phone, @CreatedAt)");
            cmd.Parameters.AddWithValue("@Name", player.Name);
            cmd.Parameters.AddWithValue("@LastName", player.LastName);
            cmd.Parameters.AddWithValue("@Level", player.Level);
            cmd.Parameters.AddWithValue("@Country", player.Country);
            cmd.Parameters.AddWithValue("@City", player.City);
            cmd.Parameters.AddWithValue("@Club", (object?)player.Club ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Association", (object?)player.Association ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", player.Email);
            cmd.Parameters.AddWithValue("@Phone", (object?)player.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        }
        else
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"UPDATE Player SET Name = @Name, LastName = @LastName, Level = @Level, Country = @Country, City = @City, Club = @Club, Association = @Association, Email = @Email, Phone = @Phone");
            cmd.Parameters.AddWithValue("@Name", player.Name);
            cmd.Parameters.AddWithValue("@LastName", player.LastName);
            cmd.Parameters.AddWithValue("@Level", player.Level);
            cmd.Parameters.AddWithValue("@Country", player.Country);
            cmd.Parameters.AddWithValue("@City", player.City);
            cmd.Parameters.AddWithValue("@Club", (object?)player.Club ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Association", (object?)player.Association ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", player.Email);
            cmd.Parameters.AddWithValue("@Phone", (object?)player.Phone ?? DBNull.Value);
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
            cmd = await GetPlayerSqliteCommandAsync(isPlayerExists, player);
            var result = await cmd.ExecuteNonQueryAsync();
            if (result > 0)
            {
                Logger.LogInformation(isPlayerExists ? "Player has been updated successfully" : "Player has been created successfully");
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
            await EnsureMigratedAsync();
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
                Country = IsDbNull(reader, "Country") ? string.Empty : reader.GetString(reader.GetOrdinal("Country")),
                City = IsDbNull(reader, "City") ? string.Empty : reader.GetString(reader.GetOrdinal("City")),
                Club = IsDbNull(reader, "Club") ? null : reader.GetString(reader.GetOrdinal("Club")),
                Association = IsDbNull(reader, "Association") ? null : reader.GetString(reader.GetOrdinal("Association")),
                Email = IsDbNull(reader, "Email") ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
                Phone = IsDbNull(reader, "Phone") ? null : reader.GetString(reader.GetOrdinal("Phone")),
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

    private static bool IsDbNull(SqliteDataReader reader, string column)
    {
        try
        {
            var idx = reader.GetOrdinal(column);
            return reader.IsDBNull(idx);
        }
        catch { return true; }
    }
}
