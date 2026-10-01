namespace Scoreboard.Client.Models;

/// <summary>Read-only mirror of a server team. <see cref="Id"/> and <see cref="ClubId"/> are the server's ids.</summary>
public class Team
{
    public int Id { get; set; }
    public int? ClubId { get; set; }
    public int? AvatarId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset? UpdatedAt { get; set; }
}
