namespace Scoreboard.Client.Models;

public class Team
{
    public int Id { get; set; }
    public int? ClubId { get; set; }
    public int? AvatarId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset? UpdatedAt { get; set; }
}
