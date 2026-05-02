namespace BilliardIQ.Mobile.Models;

public class Game
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? OpponentName { get; set; }
    public string? Location { get; set; }
    public string? Ball { get; set; }
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public int HighestRun { get; set; }
    public double Innings { get; set; }
    public string? Notes { get; set; }
    /// <summary>JPEG thumbnail (~400×300, ≤30KB) stored directly in the database.</summary>
    public byte[]? ScoreboardThumbnail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
}
