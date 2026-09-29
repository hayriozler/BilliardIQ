namespace Scoreboard.Client.Models;

/// <summary>Sent to a control client the instant it connects to /ws, so its UI reflects the live
/// game instead of starting blank/stale. Wire shape: {"type":"state","state":{...}}.</summary>
public record ScoreboardStateSnapshot(
    string Player1DisplayName,
    string Player2DisplayName,
    int Player1Score,
    int Player2Score,
    double Player1Avg,
    double Player2Avg,
    int Player1HighRun,
    int Player2HighRun,
    int ActivePlayer,
    int CurrentPoints,
    int Inning,
    int MatchTarget,
    bool ShotClockActive,
    double ShotClockSeconds,
    double ShotClockRemaining);
