namespace Zeymera.BillardIQ.WebApp.Responses;

public record MatchStatDto(
    int Id,
    string ClientId,
    int? Player1Id,
    string Player1Name,
    int Player1Score,
    double Player1Avg,
    int Player1HighRun,
    int? Player2Id,
    string Player2Name,
    int Player2Score,
    double Player2Avg,
    int Player2HighRun,
    int Inning,
    int MatchTarget,
    int Winner,
    DateTimeOffset PlayedAt,
    DateTimeOffset RecordedAt);
