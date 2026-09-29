namespace Scoreboard.Client.Models;

public class ScoreboardState
{
    public int Id { get; set; }
    public string Player1Name { get; set; } = "";
    public string Player2Name { get; set; } = "";
    public int Player1Id { get; set; } = 1;
    public int Player2Id { get; set; } = 2;
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public int Inning { get; set; } = 1;
    public int MatchTarget { get; set; } = 40;
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }
    public int CurrentPoints { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }
    public int ShotClockSeconds { get; set; } = 40;
    public double ShotClockRemaining { get; set; } = 40;
    public bool ShotClockActive { get; set; }
    public int ActivePlayer { get; set; } = 1;

}
