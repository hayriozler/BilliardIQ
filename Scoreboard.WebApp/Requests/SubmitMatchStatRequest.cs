namespace Scoreboard.WebApp.Requests;

public record SubmitMatchStatRequest(
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
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EndedAt = null,
    bool IsHandicap = false,
    int Player1Target = 0,
    int Player2Target = 0,
    List<MatchHistoryRequest>? History = null);

public record MatchHistoryRequest(int? PlayerId, int PlayerSlot, int Inning, int Score, int TotalScore, DateTimeOffset? PlayedAt);
