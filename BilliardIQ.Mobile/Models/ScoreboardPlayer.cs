namespace BilliardIQ.Mobile.Models;

public class ScoreboardPlayer
{
    public int Id { get; set; }

    public string NickName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public byte[]? Photo { get; set; }

    public ImageSource? PhotoSource => Photo is { Length: > 0 } ? ImageSource.FromStream(() => new MemoryStream(Photo)) : null;
}
