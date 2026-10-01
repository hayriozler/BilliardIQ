namespace Scoreboard.WebApp.Responses;

/// <summary>The venue a scoreboard belongs to; the scoreboard adopts its language.</summary>
public record OrganizationDto(string Name, string Language, string? CountryCode, string Currency, string TimeZone);
