namespace Scoreboard.WebApp.Models;

/// <summary>
/// Flat match summary pushed by a kiosk. Legacy shape kept until the kiosk speaks
/// <see cref="Domain.MatchEvent"/>; Player ids are the server's own <see cref="Domain.Player.Id"/>.
/// </summary>
public class MatchStat
{
    public int Id { get; set; }
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public int? TableId { get; set; }
    public int? TableNo { get; set; }
    public BilliardTable? Table { get; set; }

    public int? DeviceId { get; set; }          // legacy: eski eşleştirme koduyla gelen kayıtlar
    public Device? Device { get; set; }

    public int? Player1ExternalId { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }

    public int? Player2ExternalId { get; set; }
    public string Player2Name { get; set; } = string.Empty;
    public int Player2Score { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }

    public int Inning { get; set; }
    public int MatchTarget { get; set; }

    public int Winner { get; set; }

    public DateTimeOffset PlayedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Length in minutes of one <see cref="MatchStatBucket"/>; 0 when no distribution was sent.</summary>
    public int BucketMinutes { get; set; }
    public List<MatchStatBucket> Buckets { get; set; } = [];

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Points scored by one player slot (1 or 2) during one time bucket of a match.</summary>
public class MatchStatBucket
{
    public int Id { get; set; }
    public int MatchStatId { get; set; }
    public MatchStat MatchStat { get; set; } = default!;
    public int PlayerSlot { get; set; }
    public int BucketIndex { get; set; }
    public int TotalPoints { get; set; }
}
