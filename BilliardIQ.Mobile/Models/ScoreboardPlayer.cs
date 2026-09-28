namespace BilliardIQ.Mobile.Models;

public class ScoreboardPlayer
{
    public int Id { get; set; }

    public int? RemoteId { get; set; }

    public string NickName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public byte[]? Photo { get; set; }

    public string? AvatarKey { get; set; }

    public int? TeamId { get; set; }

    public int? ShortcutNumber { get; set; }

    public ImageSource? PhotoSource => Photo is { Length: > 0 } ? ImageSource.FromStream(() => new MemoryStream(Photo)) : null;

    public ImageSource? DisplayImageSource => PhotoSource ?? AvatarCatalog.Find(AvatarKey)?.Source;

    // Local ids 1 and 2 are reserved for the Scoreboard page's default player 1/2 slots (see
    // ScoreboardPageModel) — these can't be deleted since the board falls back to them whenever
    // it's idle.
    public bool IsDefaultPlayer => Id is 1 or 2;
}
