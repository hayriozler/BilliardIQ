using System.Text.Json;

namespace Scoreboard.Client.Models;

public enum ScoreboardCommand
{
    ToggleControls,
    ToggleShotClock,
    ResetShotClock,
    SelectPlayer1,
    SelectPlayer2,
    IncrementPoints,
    DecrementPoints,
    CommitPoints,
    RenamePlayer1,
    RenamePlayer2,
    EndGame,
    NewGame,
    SetMatchTarget,
    ExtendMatch,
    SelectRosterPlayer1,
    SelectRosterPlayer2,
    ClearRosterPlayer1,
    ClearRosterPlayer2,
}

public record ScoreboardCommandMessage(ScoreboardCommand Command, JsonElement? Payload = null);
