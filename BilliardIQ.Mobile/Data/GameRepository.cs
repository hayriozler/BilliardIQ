using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

public class GameRepository(ILogger<GameRepository> Logger) : BaseRepo(Logger)
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
                HighestRun INTEGER NOT NULL DEFAULT 0,
                Notes TEXT,
                ScoreboardPhotoPath TEXT,
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
CREATE TABLE IF NOT EXISTS PlayerStats (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TotalMatches INTEGER NOT NULL DEFAULT 0,
                TotalScore INTEGER NOT NULL DEFAULT 0,
                TotalInnings INTEGER NOT NULL DEFAULT 0,
                Average REAL NOT NULL DEFAULT 0,
                BestAverage REAL NOT NULL DEFAULT 0,
                HighestRun INTEGER NOT NULL DEFAULT 0,
                LastMatchResult INTEGER NOT NULL DEFAULT 0,
                UpdatedAt DateTime NOT NULL
            );
";

    private readonly string[] _migrations =
    [
        "ALTER TABLE Games ADD COLUMN HighestRun INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE Games ADD COLUMN Notes TEXT",
        "ALTER TABLE Games ADD COLUMN ScoreboardPhotoPath TEXT",
        "ALTER TABLE Games ADD COLUMN Innings INTEGER NOT NULL DEFAULT 1",
        "ALTER TABLE PlayerStats ADD COLUMN TotalInnings INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE PlayerStats ADD COLUMN BestAverage REAL NOT NULL DEFAULT 0",
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

    private async Task<SqliteCommand> GetGameSqliteCommandAsync(int? Id, Game game)
    {
        await EnsureMigratedAsync();
        SqliteCommand cmd;
        if (Id is null)
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"INSERT INTO Games(OpponentName, Ball, Location, Date, PlayerScore, OpponentScore, HighestRun, Innings, Notes, ScoreboardPhotoPath, CreatedAt) VALUES (@OpponentName, @Ball, @Location, @Date, @PlayerScore, @OpponentScore, @HighestRun, @Innings, @Notes, @ScoreboardPhotoPath, @CreatedAt); SELECT last_insert_rowid();");
            cmd.Parameters.AddWithValue("@OpponentName", game.OpponentName);
            cmd.Parameters.AddWithValue("@Ball", game.Ball);
            cmd.Parameters.AddWithValue("@Location", (object?)game.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Date", game.Date);
            cmd.Parameters.AddWithValue("@PlayerScore", game.PlayerScore);
            cmd.Parameters.AddWithValue("@OpponentScore", game.OpponentScore);
            cmd.Parameters.AddWithValue("@HighestRun", game.HighestRun);
            cmd.Parameters.AddWithValue("@Innings", game.Innings < 1 ? 1 : game.Innings);
            cmd.Parameters.AddWithValue("@Notes", (object?)game.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ScoreboardPhotoPath", (object?)game.ScoreboardPhotoPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
        }
        else
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, @"UPDATE Games SET OpponentName = @OpponentName, Ball = @Ball, Location = @Location, Date = @Date, PlayerScore = @PlayerScore, OpponentScore = @OpponentScore, HighestRun = @HighestRun, Innings = @Innings, Notes = @Notes, ScoreboardPhotoPath = @ScoreboardPhotoPath Where Id=@Id");
            cmd.Parameters.AddWithValue("@Id", Id!.Value);
            cmd.Parameters.AddWithValue("@OpponentName", game.OpponentName);
            cmd.Parameters.AddWithValue("@Ball", game.Ball);
            cmd.Parameters.AddWithValue("@Location", (object?)game.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Date", game.Date);
            cmd.Parameters.AddWithValue("@PlayerScore", game.PlayerScore);
            cmd.Parameters.AddWithValue("@OpponentScore", game.OpponentScore);
            cmd.Parameters.AddWithValue("@HighestRun", game.HighestRun);
            cmd.Parameters.AddWithValue("@Innings", game.Innings < 1 ? 1 : game.Innings);
            cmd.Parameters.AddWithValue("@Notes", (object?)game.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ScoreboardPhotoPath", (object?)game.ScoreboardPhotoPath ?? DBNull.Value);
        }
        return cmd;
    }

    public async Task<Game?> UpsertGameAsync(int? Id, Game game)
    {
        int? gameId = Id; // Id is the existing game's PK; game.Id is always 0 (new object)

        // Step 1: INSERT or UPDATE — close command before reading back or recalculating
        {
            SqliteCommand? cmd = null;
            try
            {
                cmd = await GetGameSqliteCommandAsync(gameId, game);
                if (gameId is null)
                {
                    var scalar = await cmd.ExecuteScalarAsync();
                    if (scalar != null && scalar != DBNull.Value && int.TryParse(scalar.ToString(), out var newId))
                    {
                        gameId = newId;
                        Logger.LogInformation("Game created with Id {Id}", gameId);
                    }
                    else
                    {
                        Logger.LogWarning("Failed to retrieve new game Id after insertion");
                        return null;
                    }
                }
                else
                {
                    await cmd.ExecuteNonQueryAsync();
                    Logger.LogInformation("Game {Id} updated", gameId);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error upserting game");
                throw;
            }
            finally
            {
                Disposer.Dispose(ref cmd); // connection fully closed before next steps
            }
        }

        // Step 2: read result and recalculate stats on fresh connections
        var result = await GetGameById(gameId!.Value);
        await RecalculateStatsAsync();
        return result;
    }

    private static bool IsDbNull(object value) => value is DBNull || value == null;

    public async Task<IReadOnlyList<Game>> GetGamesAsync(int limit = 15)
    {
        SqliteCommand? cmd = null;
        var result = new List<Game>();
        try
        {
            await EnsureMigratedAsync();
            using var reader = await GetSqliteReaderAsync(_tableCreationSql, $"Select * From Games order by Date Desc Limit {limit}", Enumerable.Empty<SqliteParameter>().ToList());
            if (!reader.HasRows)
            {
                return result.AsReadOnly();
            }
            while (await reader.ReadAsync())
            {
                result.Add(MapGame(reader));
            }
            return result.AsReadOnly();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error fetching games");
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
                Logger.LogInformation("Photo has been deleted successfully");
            else
                Logger.LogDebug("Something went wrong....");
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
                Logger.LogInformation("Photo has been added successfully");
            else
                Logger.LogDebug("Something went wrong....");
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
            await EnsureMigratedAsync();
            var sqlParameters = new List<SqliteParameter> { new("@Id", Id) };
            using var reader = await GetSqliteReaderAsync(_tableCreationSql, @"Select * From Games Where Id = @Id", sqlParameters);
            while (await reader.ReadAsync())
            {
                result = MapGame(reader);
            }
            return result;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error fetching game by id");
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    public async Task<PlayerSummaryStats> GetStatsAsync()
    {
        SqliteCommand? cmd = null;
        try
        {
            await EnsureMigratedAsync();
            using var reader = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT TotalMatches, Average, BestAverage, HighestRun FROM PlayerStats LIMIT 1",
                Enumerable.Empty<SqliteParameter>().ToList());
            if (await reader.ReadAsync() && reader.HasRows)
            {
                return new PlayerSummaryStats
                {
                    TotalMatches = Convert.ToInt32(reader["TotalMatches"]),
                    Average      = Convert.ToDouble(reader["Average"]),
                    BestAverage  = Convert.ToDouble(reader["BestAverage"]),
                    HighestRun   = Convert.ToInt32(reader["HighestRun"]),
                };
            }
            return new PlayerSummaryStats();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error fetching stats");
            return new PlayerSummaryStats();
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    private async Task RecalculateStatsAsync()
    {
        await EnsureMigratedAsync();

        // Step 1: read aggregate — close reader before writing
        int totalMatches = 0, totalInnings = 0, highestRun = 0;
        double average = 0;
        {
            using var r = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT COUNT(*) AS TotalMatches, COALESCE(SUM(PlayerScore),0) AS TotalScore, COALESCE(SUM(Innings),0) AS TotalInnings, COALESCE(MAX(HighestRun),0) AS HighestRun FROM Games",
                []);
            if (await r.ReadAsync())
            {
                totalMatches  = Convert.ToInt32(r["TotalMatches"]);
                var totalScore = Convert.ToInt32(r["TotalScore"]);
                totalInnings  = Convert.ToInt32(r["TotalInnings"]);
                highestRun    = Convert.ToInt32(r["HighestRun"]);
                // Overall average = total caramboles / total innings (3-cushion standard)
                average = totalInnings > 0 ? Math.Round((double)totalScore / totalInnings, 3) : 0;
            }
        }

        // Step 1b: best single-game average (max PlayerScore/Innings per game)
        double bestAverage = 0;
        {
            using var r = await GetSqliteReaderAsync(_tableCreationSql,
                "SELECT PlayerScore, Innings FROM Games WHERE Innings > 0",
                []);
            while (await r.ReadAsync())
            {
                var score   = Convert.ToInt32(r["PlayerScore"]);
                var innings = Convert.ToInt32(r["Innings"]);
                if (innings > 0)
                    bestAverage = Math.Max(bestAverage, Math.Round((double)score / innings, 3));
            }
        }

        // Step 2: upsert single stats row (always Id=1) — all readers closed
        SqliteCommand? cmd = null;
        try
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql,
                @"INSERT OR REPLACE INTO PlayerStats(Id, TotalMatches, TotalScore, TotalInnings, Average, BestAverage, HighestRun, UpdatedAt)
                  VALUES(1, @TotalMatches, @TotalScore, @TotalInnings, @Average, @BestAverage, @HighestRun, @Now)");
            cmd.Parameters.AddWithValue("@TotalMatches", totalMatches);
            cmd.Parameters.AddWithValue("@TotalScore", totalInnings > 0 ? (int)(average * totalInnings) : 0);
            cmd.Parameters.AddWithValue("@TotalInnings", totalInnings);
            cmd.Parameters.AddWithValue("@Average", average);
            cmd.Parameters.AddWithValue("@BestAverage", bestAverage);
            cmd.Parameters.AddWithValue("@HighestRun", highestRun);
            cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error recalculating stats");
        }
        finally
        {
            Disposer.Dispose(ref cmd);
        }
    }

    internal async Task<bool> DeleteGame(int? id)
    {
        if (id is null)
        {
            Logger.LogWarning("Game Id is null. Cannot delete game.");
            return false;
        }

        // Step 1: delete records — connection fully closed before stats update
        await DeleteGameRecordsAsync(id.Value);

        // Step 2: recalculate stats on a fresh connection
        await RecalculateStatsAsync();
        return true;
    }

    private async Task DeleteGameRecordsAsync(int id)
    {
        SqliteCommand? cmd = null;
        SqliteTransaction? transaction = null;
        try
        {
            cmd = await GetSqliteCommandAsync(_tableCreationSql, "DELETE FROM GamePhotos WHERE GameId = @Id");
            transaction = cmd.Connection?.BeginTransaction();
            cmd.Transaction = transaction;
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "DELETE FROM GameStats WHERE GameId = @Id";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "DELETE FROM Games WHERE Id = @Id";
            await cmd.ExecuteNonQueryAsync();
            transaction?.Commit();
            Logger.LogInformation("Game {Id} deleted", id);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error deleting game {Id}", id);
            transaction?.Rollback();
            throw;
        }
        finally
        {
            Disposer.Dispose(ref cmd); // connection closed here before RecalculateStatsAsync runs
        }
    }

    private Game MapGame(SqliteDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        OpponentName = reader["OpponentName"].ToString(),
        Ball = reader["Ball"].ToString(),
        Location = GameRepository.IsDbNull(reader["Location"]) ? null : reader["Location"].ToString(),
        Date = GameRepository.IsDbNull(reader["Date"]) ? DateTime.MinValue : Convert.ToDateTime(reader["Date"]),
        PlayerScore = GameRepository.IsDbNull(reader["PlayerScore"]) ? 0 : Convert.ToInt32(reader["PlayerScore"]),
        OpponentScore = GameRepository.IsDbNull(reader["OpponentScore"]) ? 0 : Convert.ToInt32(reader["OpponentScore"]),
        HighestRun = GameRepository.IsDbNull(reader["HighestRun"]) ? 0 : Convert.ToInt32(reader["HighestRun"]),
        Innings = GameRepository.IsDbNull(reader["Innings"]) ? 1 : Convert.ToInt32(reader["Innings"]),
        Notes = GameRepository.IsDbNull(reader["Notes"]) ? null : reader["Notes"].ToString(),
        ScoreboardPhotoPath = GameRepository.IsDbNull(reader["ScoreboardPhotoPath"]) ? null : reader["ScoreboardPhotoPath"].ToString(),
    };
}
