namespace Scoreboard.WebApp.Domain;

public class PlayerStats
{
    public int PlayerId { get; set; }
    public Discipline Discipline { get; set; }
    public StatsScope Scope { get; set; }
    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int TotalScore { get; set; }
    public int TotalInnings { get; set; }
    public decimal GeneralAverage { get; set; }
    public decimal BestGameAverage { get; set; }
    public int? BestGameAverageMatchId { get; set; }
    public int HighRun { get; set; }
    public int? HighRunMatchId { get; set; }
    public decimal Last10Average { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Player Player { get; set; } = default!;
}

public class PlayerOrganizationStats
{
    public int PlayerId { get; set; }
    public int OrganizationId { get; set; }
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
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Discipline Discipline { get; set; }
    public RatingSystem System { get; set; }
    public int? MatchId { get; set; }
    public decimal RatingBefore { get; set; }
    public decimal RatingAfter { get; set; }
    public decimal Delta { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public Player Player { get; set; } = default!;
    public Match? Match { get; set; }
}

