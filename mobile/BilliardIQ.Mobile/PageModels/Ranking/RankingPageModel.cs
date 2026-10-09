using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.Ranking;

public sealed record RankingItem(int Rank, int PlayerId, string Name, string Subtitle, string Value, string ValueLabel, ImageSource? Image)
{
    public bool HasImage => Image is not null;

    public bool HasNoImage => Image is null;

    public string Initial => Name.Length > 0 ? Name[..1].ToUpperInvariant() : "?";
}

public partial class RankingPageModel(ApiClient api, AvatarImageService avatars) : BasePageModel
{
    private static readonly string[] _periodKeys = ["30", "90", "6m", "year", "all"];
    private static readonly string[] _sortKeys = ["average", "wins", "highrun", "matches", "winpercent"];

    private bool _loaded;

    public IReadOnlyList<string> PeriodNames =>
        [L["Rank_Period30"], L["Rank_Period90"], L["Rank_Period6m"], L["Rank_PeriodYear"], L["Rank_PeriodAll"]];

    public IReadOnlyList<string> SortNames =>
        [L["Rank_SortAverage"], L["Rank_SortWins"], L["Rank_SortHighRun"], L["Rank_SortMatches"], L["Rank_SortWinPercent"]];

    [ObservableProperty]
    public partial int PeriodIndex { get; set; } = 2;

    [ObservableProperty]
    public partial int SortIndex { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<RankingItem> Items { get; set; } = [];

    [ObservableProperty]
    public partial StatsOverviewDto Overview { get; set; } = new(0, 0, 0, null, null, null, null);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasError => ErrorMessage.Length > 0;

    public string TopAverageText => LeaderText(Overview.TopAverage, r => r.AveragePerInning.ToString("F3"));

    public string TopHighRunText => LeaderText(Overview.TopHighRun, r => r.BestHighRun.ToString());

    public string MostWinsText => LeaderText(Overview.MostWins, r => r.Wins.ToString());

    public string MostMatchesText => LeaderText(Overview.MostMatches, r => r.Matches.ToString());

    partial void OnPeriodIndexChanged(int value)
    {
        if (_loaded)
        {
            _ = LoadAsync();
        }
    }

    partial void OnSortIndexChanged(int value)
    {
        if (_loaded)
        {
            _ = LoadAsync();
        }
    }

    partial void OnOverviewChanged(StatsOverviewDto value)
    {
        OnPropertyChanged(nameof(TopAverageText));
        OnPropertyChanged(nameof(TopHighRunText));
        OnPropertyChanged(nameof(MostWinsText));
        OnPropertyChanged(nameof(MostMatchesText));
    }

    [RelayCommand]
    private Task OpenPlayer(int playerId) => Shell.Current.GoToAsync("playerhome", new Dictionary<string, object> { ["playerId"] = playerId });

    [RelayCommand]
    private async Task Appearing()
    {
        await LoadAsync();
        _loaded = true;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var period = _periodKeys[Math.Clamp(PeriodIndex, 0, _periodKeys.Length - 1)];
            var sort = _sortKeys[Math.Clamp(SortIndex, 0, _sortKeys.Length - 1)];

            Overview = await api.GetAsync<StatsOverviewDto>($"api/mobile/stats/overview?period={period}");
            var rows = await api.GetAsync<List<RankingRowDto>>($"api/mobile/stats/ranking?period={period}&sort={sort}&minMatches=1&limit=200");
            Items = await Task.WhenAll(rows.Select(r => ToItemAsync(r, sort)));
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            Items = [];
            ErrorMessage = ApiErrorText.For(ex, L);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<RankingItem> ToItemAsync(RankingRowDto r, string sort)
    {
        var (value, label) = sort switch
        {
            "wins" => (r.Wins.ToString(), L["Rank_SortWins"]),
            "highrun" => (r.BestHighRun.ToString(), L["Rank_SortHighRun"]),
            "matches" => (r.Matches.ToString(), L["Rank_SortMatches"]),
            "winpercent" => ($"{r.WinPercent:F1}%", L["Rank_SortWinPercent"]),
            _ => (r.AveragePerInning.ToString("F3"), L["Rank_SortAverage"])
        };
        var subtitle = $"{r.Matches} {L["Stats_Matches"]} · {r.Wins}-{r.Losses} · {L["Stats_HighRun"]} {r.BestHighRun}";
        return new RankingItem(r.Rank, r.PlayerId, r.Name, subtitle, value, label, await avatars.GetAsync(r.AvatarId, r.PhotoUrl));
    }

    private string LeaderText(RankingRowDto? row, Func<RankingRowDto, string> value) =>
        row is null ? "-" : $"{row.Name} ({value(row)})";
}
