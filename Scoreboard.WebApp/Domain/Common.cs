namespace Scoreboard.WebApp.Domain;

/*
 * Konvansiyonlar
 *  - Id: Guid (offline tablet ID'yi kendisi üretebilir → Guid.CreateVersion7()).
 *  - Zaman: DateTimeOffset (UTC saklanır, gösterimde Organization.TimeZone).
 *  - Para: decimal (precision 18,2).
 *  - Snapshot / value object'ler EF Core'da Owned Type veya JSON column olarak map edilir.
 */

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; } // soft delete
}

/// <summary>Tenant'a (Organization) bağlı entity'ler</summary>
public interface ITenantScoped
{
    Guid OrganizationId { get; set; }
}

// ---------- Value objects ----------

public class Address
{
    public string Line1 { get; set; } = default!;
    public string? Line2 { get; set; }
    public string? District { get; set; }   // ilçe
    public string City { get; set; } = default!; // il
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "TR";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class OpeningHours
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; } // gece yarısını geçebilir (02:00)
    public bool IsClosed { get; set; }
}

public class PricingTimeSlot
{
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public decimal HourlyRate { get; set; }
}
