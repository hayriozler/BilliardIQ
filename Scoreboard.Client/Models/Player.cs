namespace Scoreboard.Client.Models;

public class Player
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "";
    public string Name { get; set; } = "";
    public int? ShortcutNumber { get; set; }
    public string? PhotoPath { get; set; }
    public int? AvatarId { get; set; }
    public int? TeamId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsSystem { get; set; }

    public int? SystemSlot { get; set; }

    public bool IsLocalSeed => IsSystem && UpdatedAt is null;
}
