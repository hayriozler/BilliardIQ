namespace Scoreboard.Client.Models;

/// <summary>
/// Read-only mirror of a server player. <see cref="Id"/> is the server's id (never generated locally) and
/// <see cref="TeamId"/> the server's team id.
/// Player 1 and Player 2 (<see cref="IsSystem"/>, <see cref="SystemSlot"/> 1 and 2) are what the board shows when nobody is
/// picked: the server's system players when a remote server is configured, otherwise local rows with Id 1 and 2
/// (<see cref="IsLocalSeed"/>) created when the database is set up.
/// </summary>
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

    /// <summary>A locally created Player 1 / Player 2: it never came from a server.</summary>
    public bool IsLocalSeed => IsSystem && UpdatedAt is null;
}
