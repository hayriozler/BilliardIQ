namespace Zeymera.BillardIQ.Client.Models;

public enum ScoreboardCommand
{
    GetState,
    ToggleControls,
    ToggleShotClock,
    ResetShotClock,
    SelectPlayer1,
    SelectPlayer2,
    IncrementPoints,
    DecrementPoints,
    AdjustPoints,
    CommitPoints,
    RenamePlayer1,
    RenamePlayer2,
    UpsertPlayer,
    AddPlayer = UpsertPlayer,
    EndGame,
    NewGame,
    SetMatchTarget,
    SelectRosterPlayer1,
    SelectRosterPlayer2,
    ClearRosterPlayer1,
    ClearRosterPlayer2,
    WarmUp,
    AddTeam,
    SetPlayer1,
    SetPlayer2,
    DeletePlayer,
    DeleteTeam,
}

public static class ScoreboardCommandReplyPolicy
{
    private static readonly HashSet<ScoreboardCommand> _excludeSenderFromBroadcast =
    [
        ScoreboardCommand.IncrementPoints,
        ScoreboardCommand.DecrementPoints,
        ScoreboardCommand.UpsertPlayer,
        ScoreboardCommand.AddTeam,
        ScoreboardCommand.SetPlayer1,
        ScoreboardCommand.SetPlayer2,
        ScoreboardCommand.DeletePlayer,
        ScoreboardCommand.DeleteTeam,
    ];

    public static bool ExcludesSender(ScoreboardCommand command) => _excludeSenderFromBroadcast.Contains(command);
}
