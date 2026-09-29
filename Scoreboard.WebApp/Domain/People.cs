namespace Scoreboard.WebApp.Domain;

/// <summary>Oyuncu profili. Global — salonlar arası taşınır.</summary>
public class Player : BaseEntity
{
    public Guid? UserId { get; set; }            // hesapsız misafir oyuncu olabilir
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Nickname { get; set; }
    public string DisplayName { get; set; } = default!; // scoreboard: 'M. YILMAZ'
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Nationality { get; set; }     // 'TR'
    public string? PhotoUrl { get; set; }
    public Handedness? Handedness { get; set; }
    public string? FederationLicenseNo { get; set; } // TBF lisans no
    public string? UmbPlayerId { get; set; }
    public int? DefaultTargetPoints { get; set; }    // serbest maç handikapı
    public bool IsGuest { get; set; }
    public Guid? CreatedInOrganizationId { get; set; }
    public bool IsPublicProfile { get; set; }

    public User? User { get; set; }
    public ICollection<ClubMembership> ClubMemberships { get; set; } = [];
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
    public ICollection<CustomerMembership> CustomerMemberships { get; set; } = [];
    public ICollection<MatchParticipant> MatchParticipations { get; set; } = [];
    public ICollection<TournamentEntry> TournamentEntries { get; set; } = [];
    public ICollection<PlayerStats> Stats { get; set; } = [];
    public ICollection<RatingHistory> RatingHistory { get; set; } = [];
}

public class Club : BaseEntity
{
    public Guid? OrganizationId { get; set; }    // bağımsız kulüp olabilir
    public string Name { get; set; } = default!;
    public string ShortName { get; set; } = default!; // scoreboard: 'KBSK'
    public string? LogoUrl { get; set; }
    public int? FoundedYear { get; set; }
    public string? FederationClubNo { get; set; }
    public string? City { get; set; }
    public string? PrimaryColor { get; set; }    // '#C8102E'
    public bool IsActive { get; set; } = true;

    public Organization? Organization { get; set; }
    public ICollection<ClubMembership> Memberships { get; set; } = [];
    public ICollection<Team> Teams { get; set; } = [];
}

public class ClubMembership : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid PlayerId { get; set; }
    public ClubMembershipRole Role { get; set; }
    public string? LicenseSeason { get; set; }   // '2026-2027'
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }

    public Club Club { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

public class Team : BaseEntity
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = default!;      // 'Kadıköy BSK A'
    public string ShortName { get; set; } = default!;
    public Guid? SeasonId { get; set; }               // kadro sezona bağlı
    public Guid? HomeOrganizationId { get; set; }
    public string? LogoUrl { get; set; }

    public Club Club { get; set; } = default!;
    public Season? Season { get; set; }
    public Organization? HomeOrganization { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
}

public class TeamMember : BaseEntity
{
    public Guid TeamId { get; set; }
    public Guid PlayerId { get; set; }
    public TeamMemberRole Role { get; set; }
    public int? BoardOrder { get; set; }         // 1.-4. masa sırası
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }

    public Team Team { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

/// <summary>Salon müşteri üyeliği (kulüp üyeliğinden bağımsız)</summary>
public class CustomerMembership : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid PlayerId { get; set; }
    public string MemberNo { get; set; } = default!;
    public MembershipTier Tier { get; set; }
    public decimal DiscountPercent { get; set; } // 0-100
    public decimal PrepaidBalance { get; set; }
    public int? PrepaidMinutes { get; set; }     // saat paketi
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public Player Player { get; set; } = default!;
}
