namespace Scoreboard.Client.Models;

public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string? City { get; set; }
    public string? PrimaryColor { get; set; }
}
