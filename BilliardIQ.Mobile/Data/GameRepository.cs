using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

internal class StatsResult
{
    public int TotalMatches { get; set; }
    public double Average { get; set; }
    public double BestAverage { get; set; }
    public int HighestRun { get; set; }
}
public class GameRepository(ILogger<GameRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task<Game?> UpsertGameAsync(int? id, Game game)
    {
        Game? result;
        List<SqliteParameter> parameters = [];
        string executeSql = "INSERT INTO Games(OpponentName, Ball, Location, Date, PlayerScore, OpponentScore, HighestRun, Innings, Notes, ScoreboardThumbnail, CreatedAt) VALUES (@OpponentName, @Ball, @Location, @Date, @PlayerScore, @OpponentScore, @HighestRun, @Innings, @Notes, @ScoreboardThumbnail, @CreatedAt); SELECT last_insert_rowid();";
        int gameId = id ?? 0;
        if (gameId ==  0)
        {
            parameters.Add(new SqliteParameter("@CreatedAt", DateTime.Now));
        }
        else
        {
            if (await GetGameByIdAsync(gameId) is not null)
            {
                Logger.LogInformation("Game with Id {Id} found. Continue with insertion.", id);
                executeSql = "UPDATE Games SET OpponentName = @OpponentName, Ball = @Ball, Location = @Location, Date = @Date, PlayerScore = @PlayerScore, OpponentScore = @OpponentScore, HighestRun = @HighestRun, Innings = @Innings, Notes = @Notes, ScoreboardThumbnail = @ScoreboardThumbnail Where Id=@Id";
                parameters.Add(new SqliteParameter("@Id", gameId));
            }
        }
        parameters.Add(new SqliteParameter("@OpponentName", game.Opponent));
        parameters.Add(new SqliteParameter("@Ball", game.Ball));
        parameters.Add(new SqliteParameter("@Location", game.Location ?? (object)DBNull.Value));
        parameters.Add(new SqliteParameter("@Date", game.Date));
        parameters.Add(new SqliteParameter("@PlayerScore", game.PlayerScore));
        parameters.Add(new SqliteParameter("@OpponentScore", game.OpponentScore));
        parameters.Add(new SqliteParameter("@HighestRun", game.HighestRun));
        parameters.Add(new SqliteParameter("@Innings", game.Innings));
        parameters.Add(new SqliteParameter("@Notes", game.Notes ?? (object)DBNull.Value));
        parameters.Add(new SqliteParameter("@ScoreboardThumbnail", game.ScoreboardThumbnail ?? (object)DBNull.Value));
        var affectedRow = await dbExecutor.ExecuteAsync(executeSql, parameters);
        if (affectedRow)
        {
            Logger.LogInformation("Game upserted with Id {Id}", gameId);
        }
        else
        {
            Logger.LogDebug("Something went wrong....");
        }
        result = await GetGameByIdAsync(gameId);
        await RecalculateStatsAsync();

        return result;
    }
    public async Task<IReadOnlyList<Game>> GetGamesAsync(int limit = 15)
    {
        string sql = $"SELECT Id, OpponentName , Ball, Location, Date, PlayerScore, OpponentScore, HighestRun, Innings FROM Games ORDER BY Date DESC LIMIT @limit";
        return await dbExecutor.ReadDataAsync<Game>(sql, [new("@limit", limit)]);
    }
    public async Task AddPhotoAsync(int GameId, string PhotoName, string PhotoPath)
    {
        var parameters = new List<SqliteParameter> { new("@GameId", GameId),
                new("@PhotoName", PhotoName),
                new("@PhotoPath", PhotoPath),
                new("@CreatedAt", DateTime.UtcNow)
            };
        var result = await dbExecutor.ExecuteAsync(@"INSERT INTO GamePhotos(GameId, PhotoName, PhotoPath, CreatedAt) VALUES (@GameId, @PhotoName, @PhotoPath, @CreatedAt)", parameters);
        if (result )
            Logger.LogInformation("Photo has been added successfully");
        else
            Logger.LogDebug("Something went wrong....");
    }

    public async Task<Game?> GetGameByIdAsync(int Id) => await dbExecutor.ReadSingleDataAsync<Game>(@"Select * From Games Where Id = @Id", [new("@Id", Id)]);
    public async Task<PlayerSummaryStats> GetStatsAsync()
    {
        var playerSummaryStats = await dbExecutor.ReadSingleDataAsync<PlayerSummaryStats>("SELECT TotalMatches, Average, BestAverage, HighestRun FROM PlayerStats LIMIT 1");
        playerSummaryStats ??= new PlayerSummaryStats
        {
            TotalMatches = 0,
            Average = 0,
            BestAverage = 0,
            HighestRun = 0
        };
        return playerSummaryStats;
    }
    
    private async Task RecalculateStatsAsync()
    {        
        var result = await dbExecutor.ReadSingleDataAsync<StatsResult>(
                "SELECT COUNT(*) AS TotalMatches, COALESCE(AVG(CASE WHEN Innings > 0 THEN Innings END),0) AS AvgInnings, COALESCE(MAX(Innings),0) AS BestInnings, COALESCE(MAX(HighestRun),0) AS HighestRun FROM Games");
        result ??= new StatsResult
            {
                TotalMatches = 0,
                Average = 0,
                BestAverage = 0,
                HighestRun = 0
            };
        var parameters = new List<SqliteParameter>
            {
                new("@TotalMatches", result.TotalMatches),
                new("@Average", result.Average),
                new("@BestAverage", result.BestAverage),
                new("@HighestRun", result.HighestRun),
                new("@Now", DateTime.UtcNow)
            };
        await dbExecutor.ExecuteAsync(
            @"INSERT OR REPLACE INTO PlayerStats(Id, TotalMatches, TotalScore, TotalInnings, Average, BestAverage, HighestRun, UpdatedAt)
                  VALUES(1, @TotalMatches, 0, 0, @Average, @BestAverage, @HighestRun, @Now)", parameters);
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
        await dbExecutor.ExecuteAsync("""
                DELETE FROM GamePhotos WHERE GameId = @Id;
                DELETE FROM GameStats WHERE GameId = @Id;
                DELETE FROM Games WHERE Id = @Id;
                """,
                [new("@Id", id)]);
        Logger.LogInformation("Game {Id} deleted", id);
    }
}

