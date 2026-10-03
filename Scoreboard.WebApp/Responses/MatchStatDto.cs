namespace Scoreboard.WebApp.Responses;

public record MatchStatDto(
    int Id,
    int? TableNo,
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
    bool IsHandicap,
    int Player1Target,
    int Player2Target,
    int Winner,
    DateTimeOffset PlayedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset RecordedAt,
    int ScoreDistributionBucketMinutes,
    List<ScoreBucketDto> ScoreDistribution);

public record ScoreBucketDto(int PlayerSlot, int BucketIndex, int TotalPoints);
