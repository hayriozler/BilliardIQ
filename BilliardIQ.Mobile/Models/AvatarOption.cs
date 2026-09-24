namespace BilliardIQ.Mobile.Models;

public class AvatarOption
{
    public required string Key { get; init; }

    public required string FileName { get; init; }

    public ImageSource Source => ImageSource.FromFile(FileName);
}
