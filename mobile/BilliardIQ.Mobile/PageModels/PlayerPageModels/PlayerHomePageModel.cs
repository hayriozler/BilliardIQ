using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public sealed record PlayerMatchItem(string Opponent, string ScoreText, string DateText, string AverageText, string HighRunText, bool Won);

public partial class PlayerHomePageModel(ApiClient api) : BasePageModel
{
    [ObservableProperty]
    public partial string PlayerName { get; set; } = "";

    [ObservableProperty]
    public partial StatSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    [ObservableProperty]
    public partial IReadOnlyList<PlayerMatchItem> LastMatches { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoMatches))]
    public partial bool HasMatches { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasNoMatches => !HasMatches;

    public bool HasError => ErrorMessage.Length > 0;

    [RelayCommand]
    private async Task Appearing()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var home = await api.GetAsync<PlayerHomeDto>("api/mobile/player/home");
            PlayerName = home.Player.DisplayName;
            Summary = home.Summary;
            LastMatches = home.LastMatches
                .OrderByDescending(m => m.PlayedAt)
                .Select(m => new PlayerMatchItem(
                    m.OpponentName,
                    $"{m.PlayerScore} - {m.OpponentScore}",
                    m.PlayedAt.ToLocalTime().ToString("dd.MM.yyyy"),
                    m.Average.ToString("F3"),
                    m.HighRun.ToString(),
                    m.Won))
                .ToList();
            HasMatches = LastMatches.Count > 0;
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
