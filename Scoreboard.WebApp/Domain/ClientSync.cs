namespace Scoreboard.WebApp.Domain;

public class ClientSync
{
    public int OrganizationId { get; set; }
    public int TableId { get; set; }
    public string InstanceId { get; set; } = default!;
    public string? IpAddress { get; set; }
    public bool PendingFull { get; set; }
    public string PendingKeys { get; set; } = "[]";
    public DateTimeOffset LastSyncAt { get; set; }
    public DateTimeOffset LastFullSyncAt { get; set; }
}
