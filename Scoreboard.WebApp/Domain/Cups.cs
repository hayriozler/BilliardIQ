namespace Scoreboard.WebApp.Domain;

public enum CupFormat { RoundRobin, SingleElimination }

public enum CupStatus { Draft, Running, Finished }

public enum CupRuleMode { Handicap, Fixed }

public enum CupMatchStatus { Scheduled, Finished, Bye }

public class Cup : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public CupFormat Format { get; set; }
    public CupStatus Status { get; set; }
    public int? MaxInnings { get; set; }
    public int TotalRounds { get; set; }

    public Organization Organization { get; set; } = default!;
    public ICollection<CupParticipant> Participants { get; set; } = [];
    public ICollection<CupRuleBlock> RuleBlocks { get; set; } = [];
    public ICollection<CupMatch> Matches { get; set; } = [];
}

public class CupParticipant : BaseEntity
{
    public int CupId { get; set; }
    public int PlayerId { get; set; }
    public int? HandicapTarget { get; set; }
    public int? Seed { get; set; }

    public Cup Cup { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

public class CupRuleBlock : BaseEntity
{
    public int CupId { get; set; }
    public int FromRound { get; set; }
    public int ToRound { get; set; }
    public CupRuleMode Mode { get; set; }
    public int? FixedTarget { get; set; }

    public Cup Cup { get; set; } = default!;
}

public class CupMatch : BaseEntity
{
    public int CupId { get; set; }
    public int Round { get; set; }
    public int Number { get; set; }
    public int? ParticipantAId { get; set; }
    public int? ParticipantBId { get; set; }
    public int? TargetA { get; set; }
    public int? TargetB { get; set; }
    public int? TableId { get; set; }
    public CupMatchStatus Status { get; set; }
    public int ScoreA { get; set; }
    public int ScoreB { get; set; }
    public int Innings { get; set; }
    public int HighRunA { get; set; }
    public int HighRunB { get; set; }
    public int? WinnerParticipantId { get; set; }
    public DateTimeOffset? PlayedAt { get; set; }

    public Cup Cup { get; set; } = default!;
    public CupParticipant? ParticipantA { get; set; }
    public CupParticipant? ParticipantB { get; set; }
    public BilliardTable? Table { get; set; }
}
