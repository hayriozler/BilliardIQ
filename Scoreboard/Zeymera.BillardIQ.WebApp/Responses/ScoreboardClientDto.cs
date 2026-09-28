namespace Zeymera.BillardIQ.WebApp.Responses;

public record ScoreboardClientDto(string Id, string? Name, int? ClubId, int? TableNumber, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt);
