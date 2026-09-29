namespace Scoreboard.WebApp.Models;

public class Player
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? PhotoPath { get; set; }

    public int? AvatarId { get; set; }

    public string Email { get; set; } = string.Empty;

    public Level Level { get; set; } = Level.Intermidiate;

    public string BaseCountry { get; set; } = string.Empty;

    public string BaseCity { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}


public enum Level
{
    Beginner = 1,
    Intermidiate = 2,
    Advanced = 4,
    Professional = 8
}
