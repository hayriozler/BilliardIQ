namespace Zeymera.BillardIQ.Client.Models;

public class MatchScoreStat
{
    public int Id { get; set; }
    public int MatchResultId { get; set; }
    public int PlayerSlot { get; set; }
    public int BucketIndex { get; set; }
    public int TotalPoints { get; set; }
}
