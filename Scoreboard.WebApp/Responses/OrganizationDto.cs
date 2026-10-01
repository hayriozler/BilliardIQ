namespace Scoreboard.WebApp.Responses;

public record OrganizationDto(string Name, string Language, string? CountryCode, string Currency, string TimeZone);
