namespace Scoreboard.Client.Models;

/// <summary>Read-only mirror of a server team. <see cref="ClubId"/> is the local <see cref="Club.Id"/>.</summary>
public class Team
{
    public int Id { get; set; }
    public int? RemoteId { get; set; }
    public int? ClubId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset? UpdatedAt { get; set; }
}
