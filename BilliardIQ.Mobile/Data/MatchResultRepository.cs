using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BilliardIQ.Mobile.Data;

// See ScoreboardPlayerRepository for why nullable player ids are read as a plain int (0 = none) here.
internal class MatchResultRow
{
    public int Id { get; set; }
    public DateTime PlayedAt { get; set; }
    public int Player1Id { get; set; }
    public string Player1Name { get; set; } = "";
    public int Player1Score { get; set; }
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }
    public int Player2Id { get; set; }
    public string Player2Name { get; set; } = "";
    public int Player2Score { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }
    public int Inning { get; set; }
    public int MatchTarget { get; set; }
    public int Winner { get; set; }
    public int ScoreDistributionBucketMinutes { get; set; }
}

public class MatchResultRepository(ILogger<MatchResultRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    // Inserts the match result and its score-distribution rows on a single connection/transaction so
    // the score-stat rows can be linked with last_insert_rowid() from the just-inserted match_result row.
    public async Task InsertAsync(MatchResult result, IReadOnlyList<MatchScoreStat> scoreDistribution)
    {
        using var connection = DatabaseExecutor.GetNewDbConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        int matchResultId;
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO match_result(PlayedAt, Player1Id, Player1Name, Player1Score, Player1Avg, Player1HighRun, Player2Id, Player2Name, Player2Score, Player2Avg, Player2HighRun, Inning, MatchTarget, Winner, ScoreDistributionBucketMinutes)
VALUES(@PlayedAt, @Player1Id, @Player1Name, @Player1Score, @Player1Avg, @Player1HighRun, @Player2Id, @Player2Name, @Player2Score, @Player2Avg, @Player2HighRun, @Inning, @MatchTarget, @Winner, @ScoreDistributionBucketMinutes);
SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@PlayedAt", result.PlayedAt);
            command.Parameters.AddWithValue("@Player1Id", (object?)result.Player1Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@Player1Name", result.Player1Name);
            command.Parameters.AddWithValue("@Player1Score", result.Player1Score);
            command.Parameters.AddWithValue("@Player1Avg", result.Player1Avg);
            command.Parameters.AddWithValue("@Player1HighRun", result.Player1HighRun);
            command.Parameters.AddWithValue("@Player2Id", (object?)result.Player2Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@Player2Name", result.Player2Name);
            command.Parameters.AddWithValue("@Player2Score", result.Player2Score);
            command.Parameters.AddWithValue("@Player2Avg", result.Player2Avg);
            command.Parameters.AddWithValue("@Player2HighRun", result.Player2HighRun);
            command.Parameters.AddWithValue("@Inning", result.Inning);
            command.Parameters.AddWithValue("@MatchTarget", result.MatchTarget);
            command.Parameters.AddWithValue("@Winner", result.Winner);
            command.Parameters.AddWithValue("@ScoreDistributionBucketMinutes", result.ScoreDistributionBucketMinutes);
            matchResultId = Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        foreach (var stat in scoreDistribution)
        {
            using var statCommand = connection.CreateCommand();
            statCommand.Transaction = transaction;
            statCommand.CommandText = @"INSERT INTO match_score_stat(MatchResultId, PlayerSlot, BucketIndex, TotalPoints) VALUES(@MatchResultId, @PlayerSlot, @BucketIndex, @TotalPoints);";
            statCommand.Parameters.AddWithValue("@MatchResultId", matchResultId);
            statCommand.Parameters.AddWithValue("@PlayerSlot", stat.PlayerSlot);
            statCommand.Parameters.AddWithValue("@BucketIndex", stat.BucketIndex);
            statCommand.Parameters.AddWithValue("@TotalPoints", stat.TotalPoints);
            await statCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        Logger.LogInformation("Match result {Id} recorded with {Count} score-distribution rows", matchResultId, scoreDistribution.Count);
    }

    public async Task<IReadOnlyList<MatchResult>> GetAllAsync()
    {
        var rows = await dbExecutor.ReadDataAsync<MatchResultRow>(
            @"SELECT Id, PlayedAt, Player1Id, Player1Name, Player1Score, Player1Avg, Player1HighRun,
                     Player2Id, Player2Name, Player2Score, Player2Avg, Player2HighRun, Inning, MatchTarget, Winner, ScoreDistributionBucketMinutes
              FROM match_result ORDER BY PlayedAt DESC");

        return rows.Select(r => new MatchResult
        {
            Id = r.Id,
            PlayedAt = r.PlayedAt,
            Player1Id = r.Player1Id == 0 ? null : r.Player1Id,
            Player1Name = r.Player1Name,
            Player1Score = r.Player1Score,
            Player1Avg = r.Player1Avg,
            Player1HighRun = r.Player1HighRun,
            Player2Id = r.Player2Id == 0 ? null : r.Player2Id,
            Player2Name = r.Player2Name,
            Player2Score = r.Player2Score,
            Player2Avg = r.Player2Avg,
            Player2HighRun = r.Player2HighRun,
            Inning = r.Inning,
            MatchTarget = r.MatchTarget,
            Winner = r.Winner,
            ScoreDistributionBucketMinutes = r.ScoreDistributionBucketMinutes,
        }).ToList();
    }
}
