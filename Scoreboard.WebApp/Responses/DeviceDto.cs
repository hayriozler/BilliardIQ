namespace Scoreboard.WebApp.Responses;

public record DeviceDto(
    int Id,
    string? PairingCode,
    string Name,
    int? TableId,
    int? TableNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt);
