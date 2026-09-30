namespace Scoreboard.Client.Models;

/// <summary>
/// Read-only mirror of a server player. Local rows Id 1 and 2 are the placeholder players the board uses when
/// no roster player is selected: seeded locally, and once the server's system players (<see cref="IsSystem"/>,
/// <see cref="SystemSlot"/> 1 and 2) have been pulled they take over those two rows.
/// <see cref="TeamId"/> is the local <see cref="Team.Id"/>.
/// </summary>
public class Player
{
    public int Id { get; set; }
    public int? RemoteId { get; set; }
    public string Nickname { get; set; } = "";
    public string Name { get; set; } = "";
    public int? ShortcutNumber { get; set; }
    public string? PhotoPath { get; set; }
    public int? AvatarId { get; set; }
    public int? TeamId { get; set; }
    public int? Level { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? LicenseNo { get; set; }
    public DateOnly? LicenseValidUntil { get; set; }
    public string? AssociationName { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>A server-created system player (Player 1 / Player 2); never deleted by the mirror.</summary>
    public bool IsSystem { get; set; }

    public int? SystemSlot { get; set; }

    public bool IsPlaceholder => Id is 1 or 2;
}
