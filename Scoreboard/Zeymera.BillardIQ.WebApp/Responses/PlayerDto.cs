using Zeymera.BillardIQ.WebApp.Models;

namespace Zeymera.BillardIQ.WebApp.Responses;

public record PlayerDto(
    int Id,
    int ClubId,
    string Nickname,
    string Name,
    string? PhotoPath,
    int? AvatarId,
    string Email,
    Level Level,
    string BaseCountry,
    string BaseCity,
    DateTimeOffset UpdatedAt);
