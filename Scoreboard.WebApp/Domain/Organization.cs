namespace Scoreboard.WebApp.Domain;

// ============================ IDENTITY ============================

/// <summary>Sisteme login olan hesap. Player ile aynı şey DEĞİL.</summary>
public class User : BaseEntity
{
    public string? Email { get; set; }
    public string? Phone { get; set; }          // E.164: +905xxxxxxxxx
    public string? PasswordHash { get; set; }   // OTP/SSO girişte boş olabilir
    public string DisplayName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string Locale { get; set; } = "tr-TR";
    public UserStatus Status { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public bool IsPlatformAdmin { get; set; }

    public Player? Player { get; set; }
    public ICollection<StaffMember> StaffAssignments { get; set; } = [];
}

// ========================== ORGANIZATION ==========================

/// <summary>Bilardo salonu / işletme — tenant. Tüm operasyonel veriler buna bağlıdır.</summary>
[GlobalFilter(nameof(Id))]
public class Organization : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;       // /o/kadikoy-bilardo
    public string Code { get; set; } = default!;       // yönetim paneli giriş / eşleştirme kodu
    public string? ClientId { get; set; }              // scoreboard istemcilerinin X-Client-Id başlığında gönderdiği salon kimliği
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }  // VKN
    public string? TaxOffice { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public Address Address { get; set; } = new();       // owned
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public List<OpeningHours> OpeningHours { get; set; } = []; // owned / JSON
    public string Language { get; set; } = "tr";       // salon dili; ülkeden türetilir, sistem oyuncularının adlarını belirler
    public string? CountryCode { get; set; }           // kayıtta seçilen ülke (TR, NL, ...); dil, para birimi, saat dilimi ve bölgeler buna göre hazırlanır
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

/// <summary>User ↔ Organization rol ataması</summary>
public class StaffMember : BaseEntity, ITenantScoped
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public List<StaffRole> Roles { get; set; } = [];
    public string? PinCodeHash { get; set; } // kasada hızlı giriş
    public bool IsActive { get; set; } = true;
    public DateOnly? HiredAt { get; set; }

    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
}

// ========================= TABLE & DEVICE =========================

public class BilliardTable : BaseEntity, ITenantScoped
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int Number { get; set; }
    public int? ScoreboardNo { get; set; }      // scoreboard (monitör) kullanan masalarda otomatik üretilir; istemci X-Table-No olarak bunu gönderir. null → bu masada scoreboard yok
    public string? Label { get; set; }          // 'VIP 1'
    public TableType Type { get; set; }
    public string? Brand { get; set; }          // Verhoeven, Chevillotte
    public bool IsHeated { get; set; }
    public string? ClothBrand { get; set; }     // Simonis 300
    public DateOnly? ClothChangedAt { get; set; }
    public TableStatus Status { get; set; }     // denormalize
    public int? PricingRuleId { get; set; }    // null → varsayılan kural
    public int? CurrentSessionId { get; set; } // denormalize
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

/// <summary>Masaya eşlenmiş scoreboard tableti, salon TV'si veya kasa</summary>
public class Device : BaseEntity, ITenantScoped
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int? TableId { get; set; }           // Scoreboard için zorunlu
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
