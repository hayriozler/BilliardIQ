using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Models;

public static class ScoreboardStateSnapshotFactory
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ScoreboardStateSnapshot Create(ScoreboardState state, Player? player1, Player? player2) => new ScoreboardStateSnapshot(
            ResolveName(player1, state.Player1Name),
            ResolveName(player2, state.Player2Name),
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

    private static string ResolveName(Player? player, string manualName)
    {
        if (player is not null)
        {
            return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
        }
        return manualName;
    }
}
