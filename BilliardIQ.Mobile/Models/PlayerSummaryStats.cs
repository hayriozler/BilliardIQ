namespace BilliardIQ.Mobile.Models;

public class PlayerSummaryStats
{
    public int TotalMatches { get; set; }
    public double Average { get; set; }     // career: total caramboles / total innings
    public double BestAverage { get; set; } // best single-game average
    public int HighestRun { get; set; }
}
