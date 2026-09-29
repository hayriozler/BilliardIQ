namespace Scoreboard.WebApp.Models;

/// <summary>
/// Flat match summary pushed by a kiosk. Legacy shape kept until the kiosk speaks
/// <see cref="Domain.MatchEvent"/>; Player ids are the server's own <see cref="Domain.Player.Id"/>.
/// </summary>
public class MatchStat
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    public int? Player1ExternalId { get; set; }
    public string Player1Name { get; set; } = string.Empty;
    public int Player1Score { get; set; }
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }

    public int? Player2ExternalId { get; set; }
    public string Player2Name { get; set; } = string.Empty;
    public int Player2Score { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }

    public int Inning { get; set; }
    public int MatchTarget { get; set; }

    public int Winner { get; set; }

    public DateTimeOffset PlayedAt { get; set; }

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
