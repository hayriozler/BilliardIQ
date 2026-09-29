using System.Numerics;

namespace BilliardIQ.Mobile.Models;

public class Ball
{
    public BallType Type { get; set; }
    public Vector2 Position { get; set; }
}
