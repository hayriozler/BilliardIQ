namespace Scoreboard.WebApp.Models;

public class ScoreboardClient
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int? ClubId { get; set; }

    public Club? Club { get; set; }

    public int? TableNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSeenAt { get; set; }
}
