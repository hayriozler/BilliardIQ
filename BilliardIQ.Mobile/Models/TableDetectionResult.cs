using System.Numerics;

namespace BilliardIQ.Mobile.Models;

public class TableDetectionResult
{
    public List<Ball> DetectedBalls { get; set; } = new();
    public Vector2 TableCornersTopLeft { get; set; }
    public Vector2 TableCornersTopRight { get; set; }
    public Vector2 TableCornersBottomLeft { get; set; }
    public Vector2 TableCornersBottomRight { get; set; }
    public double Confidence { get; set; }
}