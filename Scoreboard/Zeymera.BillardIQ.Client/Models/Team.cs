namespace Zeymera.BillardIQ.Client.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? RemoteId { get; set; }
    public bool SyncedAPI { get; set; }
    public bool SyncedWS { get; set; }
}
