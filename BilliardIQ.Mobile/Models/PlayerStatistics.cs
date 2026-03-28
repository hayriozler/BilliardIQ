namespace BilliardIQ.Mobile.Models;

public class PlayerStatistics
{
    public int TotalInnings { get; set; }

    public int TotalPoints { get; set; }

    public int HighestRun { get; set; }

    public double Average =>
        TotalInnings == 0 ? 0 : (double)TotalPoints / TotalInnings;

    public int SuccessfulShots { get; set; }

    public int MissedShots { get; set; }

    public double Accuracy =>
        (SuccessfulShots + MissedShots) == 0
        ? 0
        : (double)SuccessfulShots /
          (SuccessfulShots + MissedShots) * 100;
}