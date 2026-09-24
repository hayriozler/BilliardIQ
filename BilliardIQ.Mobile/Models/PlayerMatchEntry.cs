using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Models;

public class PlayerMatchEntry
{
    public DateTime PlayedAt { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public int Inning { get; set; }
    public int HighRun { get; set; }
    public double Average { get; set; }
    public bool Won { get; set; }

    public Color ResultColor => Won ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");
    public string ResultLetter => LocalizationManager.Instance[Won ? "PlayerStats_Win" : "PlayerStats_Loss"];
}
