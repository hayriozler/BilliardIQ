using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Models;

public class PlayerMatchEntry
{
    public DateTime PlayedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public int OpponentScore { get; set; }
    public int Inning { get; set; }
    public int HighRun { get; set; }
    public double Average { get; set; }
    public bool Won { get; set; }

    public Color ResultColor => Won ? Color.FromArgb("#2E7D32") : Color.FromArgb("#C62828");
    public string ResultLetter => LocalizationManager.Instance[Won ? "PlayerStats_Win" : "PlayerStats_Loss"];

    public bool HasStartedAt => StartedAt is not null;
    public bool HasEndedAt => EndedAt is not null;

    public string StartedAtText => StartedAt is { } started
        ? string.Format(LocalizationManager.Instance["PlayerStats_Started"], started)
        : string.Empty;

    public string EndedAtText => EndedAt is { } ended
        ? string.Format(LocalizationManager.Instance["PlayerStats_Ended"], ended)
        : string.Empty;
}
