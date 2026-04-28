namespace BilliardIQ.Mobile.Models;

public class CountryItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
}
