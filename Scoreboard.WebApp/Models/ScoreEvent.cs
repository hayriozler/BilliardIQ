namespace Scoreboard.WebApp.Models;

public class ScoreEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int PlayerSlot { get; set; }
    public int Points { get; set; }
}

