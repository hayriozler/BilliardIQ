namespace Scoreboard.Client.Models;

/// <summary>Read-only mirror of a server club. <see cref="RemoteId"/> is the server's id.</summary>
public class Club
{
    public int Id { get; set; }
    public int? RemoteId { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string? City { get; set; }
    public string? PrimaryColor { get; set; }
}
