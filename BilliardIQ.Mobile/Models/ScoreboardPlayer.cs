namespace BilliardIQ.Mobile.Models;

// A player sent to the connected scoreboard hardware — this app is only a
// remote control for it, so nothing here is persisted in BilliardIQ's own database.
public class ScoreboardPlayer
{
    public int Id { get; set; }

    public string NickName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public byte[]? Photo { get; set; }

    public ImageSource? PhotoSource => Photo is { Length: > 0 } ? ImageSource.FromStream(() => new MemoryStream(Photo)) : null;
}
