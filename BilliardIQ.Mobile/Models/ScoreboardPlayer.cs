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
}
