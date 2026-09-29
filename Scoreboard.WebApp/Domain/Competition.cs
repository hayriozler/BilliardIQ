namespace Scoreboard.WebApp.Domain;

// =========================== TOURNAMENT ===========================

public class Tournament : BaseEntity, ITenantScoped
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public Discipline Discipline { get; set; }
    public Guid DefaultRuleSetId { get; set; }
    public TournamentStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTimeOffset? RegistrationDeadline { get; set; }
    public int? MaxEntries { get; set; }
    public decimal? EntryFee { get; set; }
    public decimal? PrizePool { get; set; }
    public decimal? MaxAverageLimit { get; set; } // katılım koşulu
    public bool IsHandicap { get; set; }
    public string? PosterUrl { get; set; }
    public bool IsPublic { get; set; }

    public Organization Organization { get; set; } = default!;
    public RuleSet DefaultRuleSet { get; set; } = default!;
    public ICollection<TournamentStage> Stages { get; set; } = [];
    public ICollection<TournamentEntry> Entries { get; set; } = [];
}

public class TournamentStage : BaseEntity
{
    public Guid TournamentId { get; set; }
    public int Order { get; set; }               // 1 = ön eleme, 2 = grup, 3 = final
    public string Name { get; set; } = default!; // 'Gruplar', 'Son 16'
    public StageType Type { get; set; }
    public Guid? RuleSetId { get; set; }         // aşamaya özel kural
    public int? QualifiersPerGroup { get; set; }
    public DateOnly? StartDate { get; set; }
    public List<Tiebreaker> Tiebreakers { get; set; } =
        [Tiebreaker.MatchPoints, Tiebreaker.GeneralAverage, Tiebreaker.HighRun];

    public Tournament Tournament { get; set; } = default!;
    public RuleSet? RuleSet { get; set; }
    public ICollection<StageGroup> Groups { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
    public ICollection<StageStanding> Standings { get; set; } = [];
}

public class StageGroup : BaseEntity
{
    public Guid StageId { get; set; }
    public string Name { get; set; } = default!; // 'A Grubu'
    public int Order { get; set; }
    public List<Guid> TableIds { get; set; } = []; // grubun oynandığı masalar

    public TournamentStage Stage { get; set; } = default!;
    public ICollection<StageStanding> Standings { get; set; } = [];
}

public class TournamentEntry : BaseEntity
{
    public Guid TournamentId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? ClubId { get; set; }            // turnuva anındaki kulübü
    public int? Seed { get; set; }
    public EntryStatus Status { get; set; }
    public int? HandicapTargetPoints { get; set; }
    public decimal? EntryAverage { get; set; }   // seri başı için
    public bool FeePaid { get; set; }
    public int? FinalRank { get; set; }
    public decimal? PrizeAmount { get; set; }

    public Tournament Tournament { get; set; } = default!;
    public Player Player { get; set; } = default!;
    public Club? Club { get; set; }
}

/// <summary>Aşama / grup puan durumu (projeksiyon)</summary>
public class StageStanding : BaseEntity
{
    public Guid StageId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid EntryId { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int MatchPoints { get; set; }
    public int TotalScore { get; set; }
    public int TotalInnings { get; set; }
    public decimal GeneralAverage { get; set; }
    public int HighRun { get; set; }
    public decimal BestGameAverage { get; set; }
    public int? Rank { get; set; }
    public bool Qualified { get; set; }

    public TournamentStage Stage { get; set; } = default!;
    public StageGroup? Group { get; set; }
    public TournamentEntry Entry { get; set; } = default!;
}

// ======================= LEAGUE / TEAM FIXTURE =======================

public class League : BaseEntity
{
    public Guid? OrganizationId { get; set; }    // federasyon ligi → null
    public string Name { get; set; } = default!; // 'İstanbul 3 Bant Takım Ligi'
    public Discipline Discipline { get; set; }
    public string? Level { get; set; }           // 'Süper Lig', '1. Lig'
    public bool IsTeamLeague { get; set; }

    public ICollection<Season> Seasons { get; set; } = [];
}

public class Season : BaseEntity
{
    public Guid LeagueId { get; set; }
    public string Name { get; set; } = default!; // '2026-2027'
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public SeasonStatus Status { get; set; }
    public Guid RuleSetId { get; set; }
    public int BoardsPerFixture { get; set; } = 4;
    public int PointsForWin { get; set; } = 2;
    public int PointsForDraw { get; set; } = 1;
    public int PointsForLoss { get; set; } = 0;

    public League League { get; set; } = default!;
    public RuleSet RuleSet { get; set; } = default!;
    public ICollection<SeasonTeam> Teams { get; set; } = [];
    public ICollection<TeamFixture> Fixtures { get; set; } = [];
}

/// <summary>Sezona katılan takım + puan durumu (projeksiyon)</summary>
public class SeasonTeam : BaseEntity
{
    public Guid SeasonId { get; set; }
    public Guid TeamId { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int BoardsWon { get; set; }
    public int BoardsLost { get; set; }
    public int LeaguePoints { get; set; }
    public decimal GeneralAverage { get; set; }
    public int? Rank { get; set; }

    public Season Season { get; set; } = default!;
    public Team Team { get; set; } = default!;
}

/// <summary>Takım vs takım müsabakası — N adet bireysel Match içerir</summary>
public class TeamFixture : BaseEntity
{
    public Guid SeasonId { get; set; }
    public int Round { get; set; }               // hafta
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public FixtureStatus Status { get; set; }
    public int HomeBoardsWon { get; set; }
    public int AwayBoardsWon { get; set; }
    public int? HomeLeaguePoints { get; set; }
    public int? AwayLeaguePoints { get; set; }
    public Guid? RefereeUserId { get; set; }
    public string? Notes { get; set; }

    public Season Season { get; set; } = default!;
    public Team HomeTeam { get; set; } = default!;
    public Team AwayTeam { get; set; } = default!;
    public Organization Organization { get; set; } = default!;
    public User? Referee { get; set; }
    public ICollection<Match> Matches { get; set; } = [];
}
