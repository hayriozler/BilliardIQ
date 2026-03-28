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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}