namespace BilliardIQ.Mobile.Models;

public class PlayerMatchStats
{
    public ScoreboardPlayer Player { get; set; } = null!;
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double AverageInnings { get; set; }
    public double AveragePerInning { get; set; }
    public int BestHighRun { get; set; }
}
