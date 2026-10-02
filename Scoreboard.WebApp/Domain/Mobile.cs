namespace Scoreboard.WebApp.Domain;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public int? OrganizationId { get; set; }
    public string TokenHash { get; set; } = default!;
    public string? DeviceName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? RotatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public User User { get; set; } = default!;
}

public class PlayerInvite : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int PlayerId { get; set; }
    public string Code { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }

    public Player Player { get; set; } = default!;
}
