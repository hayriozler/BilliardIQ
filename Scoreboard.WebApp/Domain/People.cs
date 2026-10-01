namespace Scoreboard.WebApp.Domain;

public class Association : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;

    public Organization Organization { get; set; } = default!;
}

public class Country : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public ICollection<City> Cities { get; set; } = [];
}

public class City : BaseEntity
{
    public int CountryId { get; set; }
    public string Name { get; set; } = default!;

    public Country Country { get; set; } = default!;
}

public class Region : BaseEntity
{
    public int CountryId { get; set; }
    public string Name { get; set; } = default!;

    public Country Country { get; set; } = default!;
}

public class OrganizationCountry : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int CountryId { get; set; }

    public Organization Organization { get; set; } = default!;
    public Country Country { get; set; } = default!;
}

public class OrganizationCity : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int CityId { get; set; }

    public Organization Organization { get; set; } = default!;
    public City City { get; set; } = default!;
}

public class OrganizationRegion : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int RegionId { get; set; }

    public Organization Organization { get; set; } = default!;
    public Region Region { get; set; } = default!;
}

public class Player : BaseEntity
{
    public int? UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? Nickname { get; set; }
    public int? ShortcutNumber { get; set; }
    public string DisplayName { get; set; } = default!;
    public DateOnly? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? PhotoUrl { get; set; }
    public Handedness? Handedness { get; set; }
    public string? FederationLicenseNo { get; set; }
    public DateOnly? LicenseValidUntil { get; set; }
    public int? AssociationId { get; set; }
    public int? RegionId { get; set; }
    public int? CountryId { get; set; }
    public int? CityId { get; set; }
    public bool IsSystem { get; set; }
    public int? SystemSlot { get; set; }
    public string? UmbPlayerId { get; set; }
    public int? DefaultTargetPoints { get; set; }
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
    public int? OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string ShortName { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public int? FoundedYear { get; set; }
    public string? FederationClubNo { get; set; }
    public string? City { get; set; }
    public string? PrimaryColor { get; set; }
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
    public string? LicenseSeason { get; set; }
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }

    public Club Club { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

public class Team : BaseEntity
{
    public int ClubId { get; set; }
    public string Name { get; set; } = default!;
    public string ShortName { get; set; } = default!;
    public int? SeasonId { get; set; }
    public int? HomeOrganizationId { get; set; }
    public string? LogoUrl { get; set; }
    public int? AvatarId { get; set; }

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
    public int? BoardOrder { get; set; }
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }

    public Team Team { get; set; } = default!;
    public Player Player { get; set; } = default!;
}

public class CustomerMembership : BaseEntity
{
    public int OrganizationId { get; set; }
    public int PlayerId { get; set; }
    public string MemberNo { get; set; } = default!;
    public MembershipTier Tier { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal PrepaidBalance { get; set; }
    public int? PrepaidMinutes { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public Player Player { get; set; } = default!;
}
