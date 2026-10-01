using Scoreboard.WebApp.Models;

namespace Scoreboard.WebApp.Responses;

public record PlayerDto(
    int Id,
    int? ClubId,
    string Nickname,
    string Name,
    string? PhotoPath,
    int? AvatarId,
    string Email,
    Level Level,
    string BaseCountry,
    string BaseCity,
    DateTimeOffset UpdatedAt,
    int? ShortcutNumber = null,
    string? LicenseNo = null,
    DateOnly? LicenseValidUntil = null,
    string? AssociationName = null,
    bool IsSystem = false,
    int? SystemSlot = null,
    int? AssociationId = null,
    int? RegionId = null,
    string? RegionName = null,
    int? CountryId = null,
    int? CityId = null);
