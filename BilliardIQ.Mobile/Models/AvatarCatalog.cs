namespace BilliardIQ.Mobile.Models;

public static class AvatarCatalog
{
    public static IReadOnlyList<AvatarOption> Options { get; } =
    [
        new() { Key = "fish", FileName = "avatarfish.svg" },
        new() { Key = "eagle", FileName = "avatareagle.svg" },
        new() { Key = "dragon", FileName = "avatardragon.svg" },
        new() { Key = "bird", FileName = "avatarbird.svg" },
    ];

    public static AvatarOption? Find(string? key) =>
        key is null ? null : Options.FirstOrDefault(o => o.Key == key);
}
