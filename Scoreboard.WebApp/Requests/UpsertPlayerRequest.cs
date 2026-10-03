namespace Scoreboard.WebApp.Requests;

public record UpsertPlayerRequest(
    int Id,
    string Nickname,
    string Name,
    int? AvatarId,
    string? PhotoBase64,
    string Email,
    Level Level,
    string BaseCountry,
    string BaseCity,
    int? ShortcutNumber = null,
    string? LicenseNo = null,
    DateOnly? LicenseValidUntil = null,
    int? AssociationId = null,
    int? RegionId = null,
    int? CountryId = null,
    int? CityId = null);
