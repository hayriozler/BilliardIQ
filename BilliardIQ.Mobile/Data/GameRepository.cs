using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public class GameRepository(ILogger<GameRepository> Logger):BaseRepo(Logger)
{
   readonly string _tableCreationSql = @"
            CREATE TABLE IF NOT EXISTS Games (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OpponentName TEXT NOT NULL,
                Ball TEXT NOT NULL,
                Location TEXT,
                Date DateTime NOT NULL,
                PlayerScore INTEGER NOT NULL DEFAULT 0,
                OpponentScore INTEGER NOT NULL DEFAULT 0,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GamePhotos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PhotoName TEXT NOT NULL,
                PhotoPath TEXT NOT NULL,
                CreatedAt DateTime NOT NULL
            );
CREATE TABLE IF NOT EXISTS GameStats (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                GameId INTEGER NOT NULL,
                PlayerTotal INT NOT NULL,
                OpponentTotal INT NOT NULL                
            );
";    
    
    private async Task<SqliteCommand> GetSqliteCommandAsync(int? Id, Game game)
    {
        SqliteCommand cmd;
        if (Id is null)
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"INSERT INTO Games(OpponentName, Ball, Location, Date, PlayerScore, OpponentScore, CreatedAt) VALUES (@OpponentName, @Ball, @Location, @Date, @PlayerScore, @OpponentScore, @CreatedAt); SELECT last_insert_rowid();");
            cmd.Parameters.AddWithValue("@OpponentName", game.OpponentName);
            cmd.Parameters.AddWithValue("@Ball", game.Ball);
            cmd.Parameters.AddWithValue("@Location", game.Location);
            cmd.Parameters.AddWithValue("@Date", game.Date);
            cmd.Parameters.AddWithValue("@PlayerScore", game.PlayerScore);
            cmd.Parameters.AddWithValue("@OpponentScore", game.OpponentScore);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        }
        else
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"UPDATE Games SET OpponentName = @OpponentName, Ball = @Ball, Location = @Location, Date = @Date, PlayerScore = @PlayerScore, OpponentScore = @OpponentScore Where Id=@Id");
            cmd.Parameters.AddWithValue("@Id", game.Id);
            cmd.Parameters.AddWithValue("@OpponentName", game.OpponentName);
            cmd.Parameters.AddWithValue("@Ball", game.Ball);
            cmd.Parameters.AddWithValue("@Location", game.Location);
            cmd.Parameters.AddWithValue("@Date", game.Date);
            cmd.Parameters.AddWithValue("@PlayerScore", game.PlayerScore);
            cmd.Parameters.AddWithValue("@OpponentScore", game.OpponentScore);
        }
        return cmd;
    }
    public async Task<Game?> UpsertGameAsync(int? Id, Game game)
    {
        SqliteCommand? cmd = null;
        try
        {
            int? gameId = Id is null ? null : game.Id;
            cmd = await GetSqliteCommandAsync(gameId, game);
            if (gameId is null)
            {
                var scalar = await cmd.ExecuteScalarAsync(); // Insert and get new Id;
                if (scalar != null && scalar != DBNull.Value && int.TryParse(scalar.ToString(), out var newId))
                {                    
                    Logger.LogInformation("Player has been created successfully");
                    gameId = newId;
                }
                else                {
                    Logger.LogWarning("Failed to retrieve the new game Id after insertion.");
                    gameId = 0;
                }
            }
            else
            {
                // Check if game really exists;
                var tempGame = await GetGameById(gameId.Value);
                if(tempGame is null)
                {
                    Logger.LogWarning($"Game with Id {gameId.Value} does not exist. Cannot update non-existing game.");
                    return null;
                }                
                await cmd.ExecuteNonQueryAsync();
                Logger.LogInformation("Player has been updated successfully");
                gameId = tempGame.Id;
            }
            return await GetGameById(gameId.Value);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Project table");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }
    private bool IsDbNull(object value) => value is DBNull || value == null;
    public async Task<IReadOnlyList<Game>> GetGamesAsync(int limit = 15)
    {
        SqliteCommand? cmd = null;
        var result = new List<Game>();
        try
        {
            using var reader = await GetSqliteReaderAsync(_tableCreationSql, $"Select * From Games order by Date Desc Limit {limit}", Enumerable.Empty<SqliteParameter>().ToList());
            if (!reader.HasRows)
            {
                return result.AsReadOnly();
            }
            while (await reader.ReadAsync())
            {
                result.Add(new Game
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    OpponentName = reader["OpponentName"].ToString(),
                    Ball = reader["Ball"].ToString(),
                    Location = IsDbNull(reader["Location"]) ? null : reader["Location"].ToString(),
                    Date = IsDbNull(reader["Date"]) ? DateTime.MinValue : Convert.ToDateTime(reader["Date"]),
                    PlayerScore = IsDbNull(reader["PlayerScore"]) ? 0 : Convert.ToInt32(reader["PlayerScore"]),
                    OpponentScore = IsDbNull(reader["OpponentScore"]) ? 0 : Convert.ToInt32(reader["OpponentScore"])
                });
            }
            return result.AsReadOnly();

        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Project table");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    public async Task DeletePhoto(int Id, string PhotoName)
    {
        SqliteCommand? cmd = null;
        try
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"DELETE FROM GamePhotos WHERE Id = @Id AND PhotoName = @PhotoName");
            cmd.Parameters.AddWithValue("@Id", Id);
            cmd.Parameters.AddWithValue("@PhotoName", PhotoName);
            var result = await cmd.ExecuteNonQueryAsync();
            if (result > 0)
            {
                Logger.LogInformation("Photo has been deleted successfully");
            }
            else
            {
                Logger.LogDebug("Something went wrong....");
            }

        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error deleting photo");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    public async Task AddPhotoAsync(int GameId, string PhotoName, string PhotoPath)
    {
        SqliteCommand? cmd = null;
        try
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"INSERT INTO GamePhotos(GameId, PhotoName, PhotoPath, CreatedAt) VALUES (@GameId, @PhotoName, @PhotoPath, @CreatedAt)");
            cmd.Parameters.AddWithValue("@GameId", GameId);
            cmd.Parameters.AddWithValue("@PhotoName", PhotoName);
            cmd.Parameters.AddWithValue("@PhotoPath", PhotoPath);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            var result = await cmd.ExecuteNonQueryAsync();
            if (result > 0)
            {
                Logger.LogInformation("Photo has been added successfully");
            }
            else
            {
                Logger.LogDebug("Something went wrong....");
            }
           
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error adding photo");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    public async Task<Game?> GetGameById(int Id)
    {
        Game? result = default;
        SqliteCommand? cmd = null;
        try
        {
            var sqlParameters = new List<SqliteParameter> {
             new("@Id", Id)
            };
            using var reader = await GetSqliteReaderAsync(_tableCreationSql, @"Select * From Games Where Id = @Id", sqlParameters);
            while (await reader.ReadAsync())
            {
                result = new Game
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    OpponentName = reader["OpponentName"].ToString(),
                    Ball = reader["Ball"].ToString(),
                    Location = IsDbNull(reader["Location"]) ? null : reader["Location"].ToString(),
                    Date = IsDbNull(reader["Date"]) ? DateTime.MinValue : Convert.ToDateTime(reader["Date"]),
                    PlayerScore = IsDbNull(reader["PlayerScore"]) ? 0 : Convert.ToInt32(reader["PlayerScore"]),
                    OpponentScore = IsDbNull(reader["OpponentScore"]) ? 0 : Convert.ToInt32(reader["OpponentScore"])
                };
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Project table");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    internal async Task<bool> DeleteGame(int? id)
    {
        if(id is null)
        {
            Logger.LogWarning("Game Id is null. Cannot delete game.");
            return false;
        }
        SqliteCommand? cmd = null;
        SqliteTransaction? transaction = null;
        try
        {
            //Sql lite does not support multiple sql staments in one command, so we need to execute them one by one. We can use transaction here if needed.            
            cmd = await GetSqliteCommandAsync(_tableCreationSql, "Delete From GamePhotos Where GameId = @Id");
            transaction = cmd.Connection?.BeginTransaction();
            cmd.Transaction = transaction;
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "Delete From GameStats Where GameId = @Id";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText =  "Delete From Games Where Id = @Id";
            await cmd.ExecuteNonQueryAsync();
            transaction?.Commit();
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating Project table");
            transaction?.Rollback();
            throw;
        }
        finally {
            Disposer.Dispose(ref cmd);
        }

    }
}
