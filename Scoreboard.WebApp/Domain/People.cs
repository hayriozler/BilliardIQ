namespace Scoreboard.WebApp.Domain;

/// <summary>Salonun tanımladığı dernek / federasyon. Oyuncular listeden seçer, serbest metin yazılmaz.</summary>
public class Association : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;

    public Organization Organization { get; set; } = default!;
}

/// <summary>Salonun tanımladığı ülke. Oyuncular listeden seçer.</summary>
public class Country : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;

    public Organization Organization { get; set; } = default!;
    public ICollection<City> Cities { get; set; } = [];
}

/// <summary>Salonun tanımladığı şehir; bir ülkeye bağlıdır.</summary>
public class City : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = default!;

    public Organization Organization { get; set; } = default!;
    public Country Country { get; set; } = default!;
}

/// <summary>Salonun tanımladığı bölge (ör. federasyon bölgesi). Oyuncular listeden seçer.</summary>
public class Region : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;

    public Organization Organization { get; set; } = default!;
}

/// <summary>Oyuncu profili. Global — salonlar arası taşınır.</summary>
public class Player : BaseEntity
{
    public int? UserId { get; set; }            // hesapsız misafir oyuncu olabilir
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Nickname { get; set; }
    public int? ShortcutNumber { get; set; }     // scoreboard: hızlı seçim numarası, salon içinde benzersiz
    public string DisplayName { get; set; } = default!; // scoreboard: 'M. YILMAZ'
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Nationality { get; set; }     // 'TR'
    public string? PhotoUrl { get; set; }
    public Handedness? Handedness { get; set; }
    public string? FederationLicenseNo { get; set; } // TBF lisans no
    public DateOnly? LicenseValidUntil { get; set; } // lisans geçerlilik bitiş tarihi
    public int? AssociationId { get; set; }          // bağlı dernek / federasyon (salonun tanımladığı listeden)
    public int? RegionId { get; set; }               // bağlı bölge (salonun tanımladığı listeden)
    public int? CountryId { get; set; }              // ülke (listeden); Nationality adını taşır
    public int? CityId { get; set; }                 // şehir (listeden); City adını taşır
    public bool IsSystem { get; set; }               // salon açılırken otomatik oluşan 'Oyuncu 1/2'; değiştirilemez, silinemez
    public int? SystemSlot { get; set; }             // 1 veya 2 (yalnızca sistem oyuncularında)
    public string? UmbPlayerId { get; set; }
    public int? DefaultTargetPoints { get; set; }    // serbest maç handikapı
    public bool IsGuest { get; set; }
    public int? CreatedInOrganizationId { get; set; }
    public bool IsPublicProfile { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public int? AvatarId { get; set; }
    public Level Level { get; set; } = Level.Intermidiate;

    public User? User { get; set; }
    public Association? Association { get; set; }
    public Region? Region { get; set; }
    public Country? CountryRef { get; set; }
    public City? CityRef { get; set; }
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
    public int? OrganizationId { get; set; }    // bağımsız kulüp olabilir
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
    public int ClubId { get; set; }
    public int PlayerId { get; set; }
    public ClubMembershipRole Role { get; set; }
    public string? LicenseSeason { get; set; }   // '2026-2027'
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }

    public Club Club { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

public class Team : BaseEntity
{
    public int ClubId { get; set; }
    public string Name { get; set; } = default!;      // 'Kadıköy BSK A'
    public string ShortName { get; set; } = default!;
    public int? SeasonId { get; set; }               // kadro sezona bağlı
    public int? HomeOrganizationId { get; set; }
    public string? LogoUrl { get; set; }
    public int? AvatarId { get; set; }               // hazır avatar galerisinden seçilen avatar

    public Club Club { get; set; } = default!;
    public Season? Season { get; set; }
    public Organization? HomeOrganization { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
}

public class TeamMember : BaseEntity
{
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
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
    public int OrganizationId { get; set; }
    public int PlayerId { get; set; }
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
