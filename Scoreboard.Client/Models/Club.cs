namespace Scoreboard.Client.Models;

/// <summary>Read-only mirror of a server club. <see cref="Id"/> is the server's id (never generated locally).</summary>
public class Club
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string? City { get; set; }
    public string? PrimaryColor { get; set; }
}
