namespace Scoreboard.Client.Models;

public class Player
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PhotoPath { get; set; }
    public int? AvatarId { get; set; }
    public string? AvatarName { get; set; }
    public bool SyncedAPI { get; set; }
    public bool SyncedWS { get; set; }
    public int? TeamId { get; set; }
    public int? RemoteId { get; set; }
    public int? ShortcutNumber { get; set; }
}
