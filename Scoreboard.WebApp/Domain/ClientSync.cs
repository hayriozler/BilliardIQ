namespace Scoreboard.WebApp.Domain;

public class ClientSync
{
    public int OrganizationId { get; set; }
    public int TableNo { get; set; }
    public string InstanceId { get; set; } = default!;
    public long AckedSeq { get; set; }
    public long PendingSeq { get; set; }
    public bool PendingFull { get; set; }
    public DateTimeOffset LastSyncAt { get; set; }
    public DateTimeOffset LastFullSyncAt { get; set; }
}
