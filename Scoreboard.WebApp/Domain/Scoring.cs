namespace Scoreboard.WebApp.Domain;

/// <summary>Maç kural şablonu. Maç başladığında Match.Rules'a kopyalanır.</summary>
public class RuleSet : BaseEntity
{
    [GlobalFilter(IncludeNull = true)]
    public int? OrganizationId { get; set; }    // null → sistem şablonu (UMB, TBF)
    public string Name { get; set; } = default!; // 'UMB 3 Bant 40 sayı'
    public Discipline Discipline { get; set; }
    public MatchFormat Format { get; set; }
    public int TargetPoints { get; set; }        // Points: 40 | Sets: set başı 15
    public int? SetsToWin { get; set; }          // Sets formatında: 3
    public int? InningLimit { get; set; }        // 50 istaka
    public int? ShotClockSeconds { get; set; }   // 40
    public int ExtensionsPerPlayer { get; set; }
    public int ExtensionSeconds { get; set; }
    /// <summary>Eşitleme istakası: başlayan bitirirse rakip son istakasını oynar</summary>
    public bool EqualizingInning { get; set; } = true;
    public bool PenaltyShootoutOnTie { get; set; }
    public int TimeoutsPerPlayer { get; set; }
    public int? WarmupSeconds { get; set; }
    public bool IsSystem { get; set; }

    public MatchRules ToSnapshot() => new()
    {
        RuleSetId = Id,
        Discipline = Discipline,
        Format = Format,
        TargetPoints = TargetPoints,
        SetsToWin = SetsToWin,
        InningLimit = InningLimit,
        ShotClockSeconds = ShotClockSeconds,
        ExtensionsPerPlayer = ExtensionsPerPlayer,
        ExtensionSeconds = ExtensionSeconds,
        EqualizingInning = EqualizingInning,
        PenaltyShootoutOnTie = PenaltyShootoutOnTie,
        TimeoutsPerPlayer = TimeoutsPerPlayer,
        WarmupSeconds = WarmupSeconds,
    };
}

/// <summary>Match içinde saklanan değişmez kural kopyası (owned / JSON)</summary>
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
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int? TableId { get; set; }
    public int? SessionId { get; set; }         // masa kiralaması içindeyse
    public MatchContext Context { get; set; }

    // turnuva / lig bağlamı
    public int? TournamentStageId { get; set; }
    public int? StageGroupId { get; set; }
    public int? BracketRound { get; set; }
    public int? BracketPosition { get; set; }
    public int? TeamFixtureId { get; set; }
    public int? BoardNumber { get; set; }        // takım maçında kaçıncı masa

    public MatchRules Rules { get; set; } = new(); // SNAPSHOT

    public MatchStatus Status { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int? DurationSeconds { get; set; }

    // canlı durum
    public Side? BreakingSide { get; set; }      // açılışı yapan (kaçma sonucu)
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
    public ICollection<MatchParticipant> Participants { get; set; } = []; // her zaman 2
    public ICollection<MatchSet> Sets { get; set; } = [];
    public ICollection<Inning> Innings { get; set; } = [];
    public ICollection<MatchEvent> Events { get; set; } = [];
}

/// <summary>Maçtaki bir taraf (her zaman tek oyuncu). İstatistikler burada materialize edilir.</summary>
public class MatchParticipant : BaseEntity
{
    public int MatchId { get; set; }
    public Side Side { get; set; }
    public int? PlayerId { get; set; }
    public string? GuestName { get; set; }       // player kaydı yoksa
    public BallColor BallColor { get; set; }
    public int TargetPoints { get; set; }        // handikap: tarafa özel hedef
    public int? Seed { get; set; }

    // --- projeksiyon (event log'dan hesaplanır) ---
    public int Score { get; set; }
    public int Innings { get; set; }
    public int HighRun { get; set; }             // en yüksek seri
    public decimal Average { get; set; }         // Score / Innings (3 ondalık)
    public int SetsWon { get; set; }
    public int ExtensionsUsed { get; set; }
    public int TimeoutsUsed { get; set; }
    public int? PenaltyScore { get; set; }
    public MatchResult? Result { get; set; }
    public int? MatchPoints { get; set; }        // lig/turnuva puanı (2-1-0)

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

/// <summary>Bir istaka sırası (seri) — event log'dan projeksiyon</summary>
public class Inning
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public int? SetNo { get; set; }
    public int InningNo { get; set; }            // 1'den başlar
    public Side Side { get; set; }
    public int Points { get; set; }              // 0 = kaçırdı
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public int ExtensionsUsed { get; set; }
    public bool IsEqualizing { get; set; }       // eşitleme istakası
    public int ScoreAfter { get; set; }

    public Match Match { get; set; } = default!;
}

/// <summary>
/// Asıl veri kaynağı: append-only event log. (MatchId, Seq) unique.
/// Id sunucuda üretilir; (MatchId, Seq) çiftinin unique olması sync idempotency'sini sağlar.
/// </summary>
public class MatchEvent
{
    public long Id { get; set; }
    public int MatchId { get; set; }
    public long Seq { get; set; }
    public MatchEventType Type { get; set; }
    public Side? Side { get; set; }
    /// <summary>JSON: { "points": 1 } | { "targetSeq": 57 } | { "reason": "double hit" }</summary>
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ClientTimestamp { get; set; }
    public DateTimeOffset? ServerTimestamp { get; set; }
    public int? DeviceId { get; set; }
    public int? ActorUserId { get; set; }

    public Match Match { get; set; } = default!;
}
