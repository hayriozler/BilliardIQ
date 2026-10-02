namespace Scoreboard.WebApp.Domain;

public class User : BaseEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string SecurityStamp { get; set; } = NewSecurityStamp();
    public string DisplayName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; } = "tr-TR";
    public UserStatus Status { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public bool MustChangePassword { get; set; }
    public int? OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Player? Player { get; set; }
    public ICollection<StaffMember> StaffAssignments { get; set; } = [];

    public void RotateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    private static string NewSecurityStamp() => Guid.NewGuid().ToString("N");
}

public class Organization : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? ClientId { get; set; }
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public Address Address { get; set; } = new();
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public List<OpeningHours> OpeningHours { get; set; } = [];
    public string Language { get; set; } = "tr";
    public string? CountryCode { get; set; }
    public string Currency { get; set; } = "TRY";
    public int BillingRoundingMinutes { get; set; } = 5;
    public int MinimumBillableMinutes { get; set; } = 30;
    public int? DefaultRuleSetId { get; set; }
    public bool IsActive { get; set; } = true;
    public SubscriptionPlan Plan { get; set; }
    public DateTimeOffset? PlanExpiresAt { get; set; }

    public RuleSet? DefaultRuleSet { get; set; }
    public ICollection<BilliardTable> Tables { get; set; } = [];
    public ICollection<StaffMember> Staff { get; set; } = [];
    public ICollection<Device> Devices { get; set; } = [];
    public ICollection<PricingRule> PricingRules { get; set; } = [];
    public ICollection<Club> Clubs { get; set; } = [];
}

public class StaffMember : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public List<StaffRole> Roles { get; set; } = [];
    public string? PinCodeHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? HiredAt { get; set; }

    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
}

public class BilliardTable : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int Number { get; set; }
    public int? ScoreboardNo { get; set; }
    public string? Label { get; set; }
    public TableType Type { get; set; }
    public string? Brand { get; set; }
    public bool IsHeated { get; set; }
    public string? ClothBrand { get; set; }
    public DateOnly? ClothChangedAt { get; set; }
    public TableStatus Status { get; set; }
    public int? PricingRuleId { get; set; }
    public int? CurrentSessionId { get; set; }
    public int SortOrder { get; set; }
    public double? FloorPlanX { get; set; }
    public double? FloorPlanY { get; set; }

    public Organization Organization { get; set; } = default!;
    public PricingRule? PricingRule { get; set; }
    public TableSession? CurrentSession { get; set; }
    public Device? Device { get; set; }
    public ICollection<TableSession> Sessions { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
}

public class Device : BaseEntity, ITenantScoped
{
    public int OrganizationId { get; set; }
    public int? TableId { get; set; }
    public DeviceType Type { get; set; }
    public string Name { get; set; } = default!;
    public string? PairingCode { get; set; }
    public DateTimeOffset? PairedAt { get; set; }
    public string? DeviceTokenHash { get; set; }
    public DevicePlatform? Platform { get; set; }
    public string? AppVersion { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }

    public Organization Organization { get; set; } = default!;
    public BilliardTable? Table { get; set; }
}
