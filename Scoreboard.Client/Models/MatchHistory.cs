namespace Scoreboard.Client.Models;

public class MatchHistory
{
    public int Id { get; set; }
    public int? MatchResultId { get; set; }
    public DateTime Timestamp { get; set; }
    public int PlayerId { get; set; }
    public int PlayerSlot { get; set; }
    public int Inning { get; set; }
    public int Score { get; set; }
    public int TotalScore { get; set; }
}
