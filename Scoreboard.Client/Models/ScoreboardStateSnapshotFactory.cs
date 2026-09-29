using System.Text.Json;

namespace Scoreboard.Client.Models;

public static class ScoreboardStateSnapshotFactory
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ScoreboardStateSnapshot Create(ScoreboardState state, string player1DisplayName, string player2DisplayName) => new ScoreboardStateSnapshot(
            player1DisplayName,
            player2DisplayName,
            state.Player1Score,
            state.Player2Score,
            state.Player1Avg,
            state.Player2Avg,
            state.Player1HighRun,
            state.Player2HighRun,
            state.ActivePlayer,
            state.CurrentPoints,
            state.Inning,
            state.MatchTarget,
            state.ShotClockActive,
            state.ShotClockSeconds,
            state.ShotClockRemaining);

    public static string ToWireJson(ScoreboardStateSnapshot snapshot) =>
        JsonSerializer.Serialize(new { type = "state", state = snapshot }, _jsonOptions);
}
