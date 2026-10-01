namespace Scoreboard.WebApp.Domain;

// ===================== PRICING & RESERVATION =====================

public class PricingRule : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;     // 'Maç masası standart'
    public string Currency { get; set; } = "TRY";
    public decimal DefaultHourlyRate { get; set; }
    public List<PricingTimeSlot> TimeSlots { get; set; } = []; // owned / JSON
    public decimal? PerPlayerSurcharge { get; set; }
    public decimal? MemberDiscountPercent { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = default!;
}

/// <summary>Session açıldığı andaki fiyat kuralının değişmez kopyası (owned / JSON)</summary>
public class PricingSnapshot
{
    public int PricingRuleId { get; set; }
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = "TRY";
    public decimal DefaultHourlyRate { get; set; }
    public List<PricingTimeSlot> TimeSlots { get; set; } = [];
    public decimal? PerPlayerSurcharge { get; set; }
    public decimal? MemberDiscountPercent { get; set; }
}

public class Reservation : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int? TableId { get; set; }           // null → uygun herhangi masa
    public int? PlayerId { get; set; }
    public string ContactName { get; set; } = default!;
    public string? ContactPhone { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int PartySize { get; set; } = 2;
    public ReservationStatus Status { get; set; }
    public decimal? DepositAmount { get; set; }
    public int? SessionId { get; set; }         // check-in sonrası
    public int? CreatedByUserId { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public BilliardTable? Table { get; set; }
    public Player? Player { get; set; }
    public TableSession? Session { get; set; }
}

// ========================= TABLE SESSION =========================

/// <summary>Masa kiralama — para ile ilgili. Match'ten ayrıdır.</summary>
public class TableSession : BaseEntity, ITenantScoped
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int TableId { get; set; }
    public int? ReservationId { get; set; }
    public TableSessionStatus Status { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; } // Paused durumundayken duraklatma başlangıcı
    public int PausedMinutes { get; set; }
    public int? BilledMinutes { get; set; }      // yuvarlanmış
    public PricingSnapshot PricingSnapshot { get; set; } = new(); // owned / JSON
    public decimal? TableAmount { get; set; }
    public decimal ItemsAmount { get; set; }     // denormalize
    public decimal DiscountAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int OpenedByStaffId { get; set; }
    public int? ClosedByStaffId { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }   // tahsilatı silinen (iptal edilen) oturum
    public int? VoidedByStaffId { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public BilliardTable Table { get; set; } = default!;
    public Reservation? Reservation { get; set; }
    public StaffMember OpenedByStaff { get; set; } = default!;
    public StaffMember? ClosedByStaff { get; set; }
    public ICollection<SessionPlayer> Players { get; set; } = [];
    public ICollection<Match> Matches { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

/// <summary>Session'daki oyuncular (hesap bölüşme, üye indirimi)</summary>
public class SessionPlayer : BaseEntity
{
    public int SessionId { get; set; }
    public int? PlayerId { get; set; }          // anonim müşteri olabilir
    public string? GuestName { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public int? CustomerMembershipId { get; set; }

    public TableSession Session { get; set; } = default!;
    public Player? Player { get; set; }
    public CustomerMembership? CustomerMembership { get; set; }
}

// ============================== POS ==============================

public class ProductCategory : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public string Name { get; set; } = default!;  // 'Sıcak İçecekler'
    public int SortOrder { get; set; }

    public Organization Organization { get; set; } = default!;
    public ICollection<Product> Products { get; set; } = [];
}

public class Product : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }         // %10, %20
    public bool TrackStock { get; set; }
    public decimal? StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }

    public Organization Organization { get; set; } = default!;
    public ProductCategory Category { get; set; } = default!;
}

public class OrderItem : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int? SessionId { get; set; }         // null → tezgah satışı
    public int ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public decimal UnitPriceSnapshot { get; set; }
    public decimal VatRateSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public OrderItemStatus Status { get; set; }
    public int? OrderedByStaffId { get; set; }
    public int? SessionPlayerId { get; set; }   // hesap bölüşmede kime ait
    public string? Note { get; set; }

    public TableSession? Session { get; set; }
    public Product Product { get; set; } = default!;
    public SessionPlayer? SessionPlayer { get; set; }
}

public class Payment : BaseEntity, ITenantScoped
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int? SessionId { get; set; }
    public int? SessionPlayerId { get; set; }
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ExternalRef { get; set; }     // POS / sanal POS ref
    public int ReceivedByStaffId { get; set; }
    public int? CashRegisterShiftId { get; set; }
    public DateTimeOffset PaidAt { get; set; }

    public TableSession? Session { get; set; }
    public SessionPlayer? SessionPlayer { get; set; }
    public StaffMember ReceivedByStaff { get; set; } = default!;
    public CashRegisterShift? CashRegisterShift { get; set; }
}

/// <summary>Kasa vardiyası — gün sonu mutabakatı</summary>
public class CashRegisterShift : BaseEntity
{
    [GlobalFilter]
    public int OrganizationId { get; set; }
    public int OpenedByStaffId { get; set; }
    public int? ClosedByStaffId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? Difference { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = default!;
    public ICollection<Payment> Payments { get; set; } = [];
}
