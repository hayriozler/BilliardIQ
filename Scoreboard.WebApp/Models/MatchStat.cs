using Scoreboard.WebApp.Domain;
namespace Scoreboard.WebApp.Models;

public class MatchStat
{
    public int Id { get; set; }
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public int? TableId { get; set; }
    public int? TableNo { get; set; }
    public BilliardTable? Table { get; set; }

    public int? DeviceId { get; set; }
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
    public bool IsHandicap { get; set; }
    public int Player1Target { get; set; }
    public int Player2Target { get; set; }

    public int Winner { get; set; }

    public DateTimeOffset PlayedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public int BucketMinutes { get; set; }
    public List<MatchStatBucket> Buckets { get; set; } = [];
    public List<MatchStatHistory> History { get; set; } = [];

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class MatchStatBucket
{
    public int Id { get; set; }
    public int MatchStatId { get; set; }
    public MatchStat MatchStat { get; set; } = default!;
    public int PlayerSlot { get; set; }
    public int BucketIndex { get; set; }
    public int TotalPoints { get; set; }
}

public class MatchStatHistory
{
    public int Id { get; set; }
    public int MatchStatId { get; set; }
    public MatchStat MatchStat { get; set; } = default!;
    public int? PlayerId { get; set; }
    public int PlayerSlot { get; set; }
    public int Inning { get; set; }
    public int Score { get; set; }
    public int TotalScore { get; set; }
    public DateTimeOffset? PlayedAt { get; set; }
}
