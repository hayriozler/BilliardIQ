namespace Scoreboard.WebApp.Domain;

/// <summary>Oyuncunun disiplin bazlı toplam istatistiği (projeksiyon). PK: (PlayerId, Discipline, Scope)</summary>
public class PlayerStats
{
    public Guid PlayerId { get; set; }
    public Discipline Discipline { get; set; }
    public StatsScope Scope { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int TotalScore { get; set; }
    public int TotalInnings { get; set; }
    public decimal GeneralAverage { get; set; }  // GA = TotalScore / TotalInnings
    public decimal BestGameAverage { get; set; } // BGA
    public Guid? BestGameAverageMatchId { get; set; }
    public int HighRun { get; set; }             // HR
    public Guid? HighRunMatchId { get; set; }
    public decimal Last10Average { get; set; }   // form göstergesi
    public DateTimeOffset UpdatedAt { get; set; }

    public Player Player { get; set; } = default!;
}

/// <summary>Salon bazlı istatistik (salon içi lider tablosu). PK: (PlayerId, OrganizationId, Discipline)</summary>
public class PlayerOrganizationStats
{
    public Guid PlayerId { get; set; }
    public Guid OrganizationId { get; set; }
    public Discipline Discipline { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int TotalScore { get; set; }
    public int TotalInnings { get; set; }
    public decimal GeneralAverage { get; set; }
    public decimal BestGameAverage { get; set; }
    public int HighRun { get; set; }
    public int TotalPlayMinutes { get; set; }
    public DateTimeOffset? LastPlayedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Player Player { get; set; } = default!;
    public Organization Organization { get; set; } = default!;
}

public class RatingHistory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PlayerId { get; set; }
    public Discipline Discipline { get; set; }
    public RatingSystem System { get; set; }
    public Guid? MatchId { get; set; }
    public decimal RatingBefore { get; set; }
    public decimal RatingAfter { get; set; }
    public decimal Delta { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public Player Player { get; set; } = default!;
    public Match? Match { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OrganizationId { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? ActorDeviceId { get; set; }
    public string EntityType { get; set; } = default!; // 'TableSession', 'Payment'
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;     // 'VOIDED', 'DISCOUNT_APPLIED'
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Ip { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
