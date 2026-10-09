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

public enum ExternalOutcome { Won, Lost, Draw }

public class ExternalMatch : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int PlayerId { get; set; }
    public DateOnly PlayedOn { get; set; }
    public string OpponentName { get; set; } = default!;
    public string? Venue { get; set; }
    public int Score { get; set; }
    public int OpponentScore { get; set; }
    public int Innings { get; set; }
    public int HighRun { get; set; }
    public ExternalOutcome Outcome { get; set; }

    public Player Player { get; set; } = default!;
}

public class PasswordResetCode : BaseEntity
{
    public int UserId { get; set; }
    public string CodeHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public int Attempts { get; set; }

    public User User { get; set; } = default!;
}
