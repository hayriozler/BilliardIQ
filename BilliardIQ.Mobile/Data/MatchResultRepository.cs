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
}

public class MatchResultRepository(ILogger<MatchResultRepository> Logger, DatabaseExecutor dbExecutor) : BaseRepo
{
    public async Task InsertAsync(MatchResult result)
    {
        var parameters = new List<SqliteParameter>
        {
            new("@PlayedAt", result.PlayedAt),
            new("@Player1Id", (object?)result.Player1Id ?? DBNull.Value),
            new("@Player1Name", result.Player1Name),
            new("@Player1Score", result.Player1Score),
            new("@Player1Avg", result.Player1Avg),
            new("@Player1HighRun", result.Player1HighRun),
            new("@Player2Id", (object?)result.Player2Id ?? DBNull.Value),
            new("@Player2Name", result.Player2Name),
            new("@Player2Score", result.Player2Score),
            new("@Player2Avg", result.Player2Avg),
            new("@Player2HighRun", result.Player2HighRun),
            new("@Inning", result.Inning),
            new("@MatchTarget", result.MatchTarget),
            new("@Winner", result.Winner),
        };
        var ok = await dbExecutor.ExecuteAsync(
            @"INSERT INTO match_result(PlayedAt, Player1Id, Player1Name, Player1Score, Player1Avg, Player1HighRun, Player2Id, Player2Name, Player2Score, Player2Avg, Player2HighRun, Inning, MatchTarget, Winner)
              VALUES(@PlayedAt, @Player1Id, @Player1Name, @Player1Score, @Player1Avg, @Player1HighRun, @Player2Id, @Player2Name, @Player2Score, @Player2Avg, @Player2HighRun, @Inning, @MatchTarget, @Winner)",
            parameters);

        if (ok) Logger.LogInformation("Match result recorded");
        else Logger.LogDebug("Something went wrong recording the match result");
    }

    public async Task<IReadOnlyList<MatchResult>> GetAllAsync()
    {
        var rows = await dbExecutor.ReadDataAsync<MatchResultRow>(
            @"SELECT Id, PlayedAt, Player1Id, Player1Name, Player1Score, Player1Avg, Player1HighRun,
                     Player2Id, Player2Name, Player2Score, Player2Avg, Player2HighRun, Inning, MatchTarget, Winner
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
        }).ToList();
    }
}
