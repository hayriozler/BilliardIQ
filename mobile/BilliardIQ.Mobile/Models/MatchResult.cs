namespace BillardIQ.Mobile.Models;

public class MatchResult
{
    public int Id { get; set; }
    public DateTime PlayedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? Player1Id { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }
    public int? Player2Id { get; set; }
    public string Player2Name { get; set; } = string.Empty;
    public int Player2Score { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }
    public int Inning { get; set; }
    public int MatchTarget { get; set; }
    public int Winner { get; set; }
    public int ScoreDistributionBucketMinutes { get; set; }

    // The Pi has no independent player-id scheme — it just echoes back whatever "id" the app sent
    // it in AddPlayer/SetPlayer1/SetPlayer2, so Player1Id/Player2Id here are directly comparable to
    // ScoreboardPlayer.Id (as long as that id stays stable, i.e. the player isn't deleted/re-added).
    public bool InvolvesAsPlayer1(int? id) => id is not null && Player1Id == id;
    public bool InvolvesAsPlayer2(int? id) => id is not null && Player2Id == id;
}
