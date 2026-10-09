using BilliardIQ.Mobile.Graphics;
using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public sealed record PlayerMatchItem(
    int MatchId, string Opponent, string ScoreText, string DateText, string AverageText, string HighRunText,
    bool Won, bool IsHandicap, string TargetText)
{
    public string ResultGlyph => char.ConvertFromUtf32(Won ? 0xE8DC : 0xE8DB);
}

public partial class PlayerHomePageModel(ApiClient api, SessionStore session) : BasePageModel, IQueryAttributable
{
    private static readonly string[] _periodKeys = ["30", "90", "6m", "year", "all"];

    private int? _playerId;
    private bool _loaded;
    private bool _isOther;

    public event Action? ChartUpdated;

    public IReadOnlyList<string> PeriodNames =>
        [L["Rank_Period30"], L["Rank_Period90"], L["Rank_Period6m"], L["Rank_PeriodYear"], L["Rank_PeriodAll"]];

    public TrendChartDrawable AverageDrawable { get; } = new() { ValueFormat = "F3" };

    public TrendChartDrawable HighRunDrawable { get; } = new() { ValueFormat = "F0", LineColor = Color.FromArgb("#EF6C00") };

    [ObservableProperty]
    public partial int PeriodIndex { get; set; } = 4;

    [ObservableProperty]
    public partial string PageTitle { get; set; } = LocalizationManager.Instance["PlayerHome_Title"];

    [ObservableProperty]
    public partial string PlayerName { get; set; } = "";

    [ObservableProperty]
    public partial StatSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    [ObservableProperty]
    public partial IReadOnlyList<PlayerMatchItem> Matches { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoMatches))]
    public partial bool HasMatches { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTrend))]
    public partial bool HasTrend { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasNoMatches => !HasMatches;

    public bool HasNoTrend => !HasTrend;

    public bool HasError => ErrorMessage.Length > 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("playerId", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _playerId = id;
            _isOther = true;
        }
    }

    partial void OnPeriodIndexChanged(int value)
    {
        if (_loaded)
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (_loaded)
        {
            return;
        }

        await LoadAsync();
        _loaded = true;
    }

    [RelayCommand]
    private Task OpenMatch(int matchId) => Shell.Current.GoToAsync("matchdetail", new Dictionary<string, object> { ["matchId"] = matchId });

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var playerId = _playerId ?? session.Current?.User.PlayerId;
            if (playerId is null)
            {
                var home = await api.GetAsync<PlayerHomeDto>("api/mobile/player/home");
                playerId = home.Player.Id;
            }

            _playerId = playerId;
            var period = _periodKeys[Math.Clamp(PeriodIndex, 0, _periodKeys.Length - 1)];
            var stats = await api.GetAsync<PlayerStatsDto>($"api/mobile/stats/players/{playerId}?period={period}");
            PlayerName = stats.Player.DisplayName;
            PageTitle = _isOther ? PlayerName : L["PlayerHome_Title"];
            Summary = stats.Summary;

            var ordered = stats.Matches.OrderByDescending(m => m.PlayedAt).ToList();
            Matches = ordered.Select(ToItem).ToList();
            HasMatches = Matches.Count > 0;

            var chronological = ordered.AsEnumerable().Reverse().ToList();
            AverageDrawable.Values = chronological.Select(m => m.Average).ToList();
            HighRunDrawable.Values = chronological.Select(m => (double)m.HighRun).ToList();
            HasTrend = chronological.Count >= 2;
            ChartUpdated?.Invoke();
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

    private PlayerMatchItem ToItem(MobilePlayerMatch m) => new(
        m.MatchId,
        m.OpponentName,
        $"{m.PlayerScore} - {m.OpponentScore}",
        m.PlayedAt.ToLocalTime().ToString("dd.MM.yyyy"),
        m.Average.ToString("F3"),
        m.HighRun.ToString(),
        m.Won,
        m.IsHandicap,
        m.IsHandicap ? string.Format(L["PlayerHome_Target"], m.PlayerTarget?.ToString() ?? "-", m.OpponentTarget?.ToString() ?? "-") : "");
}
