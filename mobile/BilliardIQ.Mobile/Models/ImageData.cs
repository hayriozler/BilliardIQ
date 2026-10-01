namespace BillardIQ.Mobile.Models;

public class ImageData
{
    public required string Name { get; set; }
    public ImageSource? Image { get; set; }
    public bool IsBallSelected { get; set; } = false;
}