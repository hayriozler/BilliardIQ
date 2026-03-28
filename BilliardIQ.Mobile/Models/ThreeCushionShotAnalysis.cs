namespace BilliardIQ.Mobile.Models;

public class ThreeCushionShotAnalysis
{
    public Guid Id { get; set; }

    public DateTime Timestamp { get; set; }

    //public TableState TableState { get; set; }

    public List<CushionType> CushionsHit { get; set; } = [];

    public bool FirstObjectBallHit { get; set; }

    public bool SecondObjectBallHit { get; set; }

    public int CushionCount => CushionsHit.Count;

    public bool IsPoint
    {
        get
        {
            return CushionCount >= 3 &&
                   FirstObjectBallHit &&
                   SecondObjectBallHit;
        }
    }

    public double DifficultyScore { get; set; }

    public SpinType Spin { get; set; }

    public double Power { get; set; }
}