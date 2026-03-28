namespace BilliardIQ.Mobile.Models;

public class ThreeCushionShotSuggestion
{
    public double Angle { get; set; }

    public double Power { get; set; }

    public SpinType Spin { get; set; }

    public List<CushionType> CushionPath { get; set; }

    public double SuccessProbability { get; set; }
}