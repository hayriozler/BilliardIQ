namespace BilliardIQ.Mobile.Models;

public class MatchResult
{
    public int Id { get; set; }
    public DateTime PlayedAt { get; set; }
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
}
