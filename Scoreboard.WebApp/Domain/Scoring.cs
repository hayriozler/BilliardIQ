namespace Scoreboard.WebApp.Domain;

public class RuleSet : BaseEntity
{
    public int? OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public Discipline Discipline { get; set; }
    public MatchFormat Format { get; set; }
    public int TargetPoints { get; set; }
    public int? SetsToWin { get; set; }
    public int? InningLimit { get; set; }
    public int? ShotClockSeconds { get; set; }
    public int ExtensionsPerPlayer { get; set; }
    public int ExtensionSeconds { get; set; }
    public bool EqualizingInning { get; set; } = true;
    public bool PenaltyShootoutOnTie { get; set; }
    public int TimeoutsPerPlayer { get; set; }
    public int? WarmupSeconds { get; set; }
    public bool IsSystem { get; set; }

}

public class MatchRules
{
    public int? RuleSetId { get; set; }
    public Discipline Discipline { get; set; }
    public MatchFormat Format { get; set; }
    public int TargetPoints { get; set; }
    public int? SetsToWin { get; set; }
    public int? InningLimit { get; set; }
    public int? ShotClockSeconds { get; set; }
    public int ExtensionsPerPlayer { get; set; }
    public int ExtensionSeconds { get; set; }
    public bool EqualizingInning { get; set; }
    public bool PenaltyShootoutOnTie { get; set; }
    public int TimeoutsPerPlayer { get; set; }
    public int? WarmupSeconds { get; set; }
}

public class Match : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int? TableId { get; set; }
    public int? SessionId { get; set; }
    public MatchContext Context { get; set; }

    public int? TournamentStageId { get; set; }
    public int? StageGroupId { get; set; }
    public int? BracketRound { get; set; }
    public int? BracketPosition { get; set; }
    public int? TeamFixtureId { get; set; }
    public int? BoardNumber { get; set; }

    public MatchRules Rules { get; set; } = new();

    public MatchStatus Status { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public Side? BreakingSide { get; set; }
    public Side? CurrentSide { get; set; }
    public int CurrentInning { get; set; }
    public int? CurrentSetNo { get; set; }
    public Side? WinnerSide { get; set; }

    public int? RefereeUserId { get; set; }
    public int? ScorekeeperDeviceId { get; set; }
    public long LastEventSeq { get; set; }
    public bool IsStreamed { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public BilliardTable? Table { get; set; }
    public TableSession? Session { get; set; }
    public TournamentStage? TournamentStage { get; set; }
    public StageGroup? StageGroup { get; set; }
    public TeamFixture? TeamFixture { get; set; }
    public User? Referee { get; set; }
    public Device? ScorekeeperDevice { get; set; }
    public ICollection<MatchParticipant> Participants { get; set; } = [];
    public ICollection<MatchSet> Sets { get; set; } = [];
    public ICollection<Inning> Innings { get; set; } = [];
    public ICollection<MatchEvent> Events { get; set; } = [];
}

public class MatchParticipant : BaseEntity
{
    public int MatchId { get; set; }
    public Side Side { get; set; }
    public int? PlayerId { get; set; }
    public string? GuestName { get; set; }
    public BallColor BallColor { get; set; }
    public int TargetPoints { get; set; }
    public int? Seed { get; set; }

    public int Score { get; set; }
    public int Innings { get; set; }
    public int HighRun { get; set; }
    public decimal Average { get; set; }
    public int SetsWon { get; set; }
    public int ExtensionsUsed { get; set; }
    public int TimeoutsUsed { get; set; }
    public int? PenaltyScore { get; set; }
    public MatchResult? Result { get; set; }
    public int? MatchPoints { get; set; }

    public Match Match { get; set; } = default!;
    public Player? Player { get; set; }
}

public class MatchSet : BaseEntity
{
    public int MatchId { get; set; }
    public int SetNo { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int ScoreA { get; set; }
    public int ScoreB { get; set; }
    public int InningsA { get; set; }
    public int InningsB { get; set; }
    public int HighRunA { get; set; }
    public int HighRunB { get; set; }
    public Side? WinnerSide { get; set; }

    public Match Match { get; set; } = default!;
}

public class Inning
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int? SetNo { get; set; }
    public int InningNo { get; set; }
    public Side Side { get; set; }
    public int Points { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public int ExtensionsUsed { get; set; }
    public bool IsEqualizing { get; set; }
    public int ScoreAfter { get; set; }

    public Match Match { get; set; } = default!;
}

public class MatchEvent
{
    public long Id { get; set; }
    public int MatchId { get; set; }
    public long Seq { get; set; }
    public MatchEventType Type { get; set; }
    public Side? Side { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ClientTimestamp { get; set; }
    public DateTimeOffset? ServerTimestamp { get; set; }
    public int? DeviceId { get; set; }
    public int? ActorUserId { get; set; }

    public Match Match { get; set; } = default!;
}
