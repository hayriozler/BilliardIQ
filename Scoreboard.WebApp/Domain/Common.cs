namespace Scoreboard.WebApp.Domain;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public interface ITenantScoped
{
    int OrganizationId { get; set; }
}

public class Address
{
    public string Line1 { get; set; } = default!;
    public string? Line2 { get; set; }
    public string? District { get; set; }
    public string City { get; set; } = default!;
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = "TR";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class OpeningHours
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
    public bool IsClosed { get; set; }
}

public class PricingTimeSlot
{
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public decimal HourlyRate { get; set; }
}
