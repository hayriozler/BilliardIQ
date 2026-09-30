namespace Scoreboard.WebApp.Domain;

// ===================== SALON TURNUVALARI (Turnuvalar modülü) =====================
// Sade turnuva modeli: katılımcılar salonun oyuncuları, tur aralığına göre puanlama kuralı
// (handikaplı / sabit hedef), lig usulü veya eleme usulü fikstür. Eski Tournament* varlıklarından
// bağımsızdır (onlar zorunlu RuleSet ister ve hiçbir yerde kullanılmıyor).

public enum CupFormat { RoundRobin, SingleElimination }

public enum CupStatus { Draft, Running, Finished }

/// <summary>Handicap: herkes kendi handikap sayısını çeker. Fixed: iki oyuncu da aynı sabit sayıyı çeker (ör. 40).</summary>
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
    public int? MaxInnings { get; set; }         // maç başına en fazla el (isteğe bağlı, bilgi amaçlı)
    public int TotalRounds { get; set; }         // başlatınca hesaplanır

    public Organization Organization { get; set; } = default!;
    public ICollection<CupParticipant> Participants { get; set; } = [];
    public ICollection<CupRuleBlock> RuleBlocks { get; set; } = [];
    public ICollection<CupMatch> Matches { get; set; } = [];
}

public class CupParticipant : BaseEntity
{
    public int CupId { get; set; }
    public int PlayerId { get; set; }
    public int? HandicapTarget { get; set; }     // bu turnuvadaki handikap sayısı; varsayılan Player.DefaultTargetPoints
    public int? Seed { get; set; }               // eleme usulünde seri başı sırası (1 = en güçlü)

    public Cup Cup { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

/// <summary>"1-8. turlar handikaplı, 9-10. turlar 40 çekmeli" gibi tur aralığı başına bir puanlama kuralı.</summary>
public class CupRuleBlock : BaseEntity
{
    public int CupId { get; set; }
    public int FromRound { get; set; }
    public int ToRound { get; set; }
    public CupRuleMode Mode { get; set; }
    public int? FixedTarget { get; set; }        // yalnızca Fixed modunda

    public Cup Cup { get; set; } = default!;
}

public class CupMatch : BaseEntity
{
    public int CupId { get; set; }
    public int Round { get; set; }
    public int Number { get; set; }              // turdaki sıra (1'den başlar)
    public int? ParticipantAId { get; set; }     // null → henüz belli değil (eleme) veya bay
    public int? ParticipantBId { get; set; }
    public int? TargetA { get; set; }            // tur kuralından çözülen hedef sayılar
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
