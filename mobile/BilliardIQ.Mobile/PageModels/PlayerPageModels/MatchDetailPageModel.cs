using BilliardIQ.Mobile.Graphics;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BilliardIQ.Mobile.PageModels.PlayerPageModels;

public sealed record MatchHistoryRow(int Inning, string TimeText, string Player1Points, string Player1Total, string Player2Points, string Player2Total);

public partial class MatchDetailPageModel(ApiClient api) : BasePageModel, IQueryAttributable
{
    private static readonly Color _winnerColor = Color.FromArgb("#2E7D32");
    private static readonly Color _loserColor = Colors.Gray;

    private int _matchId;

    public event Action? ChartUpdated;

    public InningsChartDrawable Player1InningsDrawable { get; } = new() { LineColor = Color.FromArgb("#1565C0") };

    public InningsChartDrawable Player2InningsDrawable { get; } = new() { LineColor = Color.FromArgb("#EF6C00") };

    public ScoringPaceChartDrawable Player1PaceDrawable { get; } = new() { RangeLabels = true, BarColor = Color.FromArgb("#1565C0") };

    public ScoringPaceChartDrawable Player2PaceDrawable { get; } = new() { RangeLabels = true, BarColor = Color.FromArgb("#EF6C00") };

    [ObservableProperty]
    public partial string Player1Name { get; set; } = "";

    [ObservableProperty]
    public partial string Player2Name { get; set; } = "";

    [ObservableProperty]
    public partial int Player1Score { get; set; }

    [ObservableProperty]
    public partial int Player2Score { get; set; }

    [ObservableProperty]
    public partial Color Player1ScoreColor { get; set; } = _loserColor;

    [ObservableProperty]
    public partial Color Player2ScoreColor { get; set; } = _loserColor;

    [ObservableProperty]
    public partial string Player1Summary { get; set; } = "";

    [ObservableProperty]
    public partial string Player2Summary { get; set; } = "";

    [ObservableProperty]
    public partial string DateText { get; set; } = "";

    [ObservableProperty]
    public partial string TimeText { get; set; } = "";

    [ObservableProperty]
    public partial string TableText { get; set; } = "";

    [ObservableProperty]
    public partial string TargetText { get; set; } = "";

    [ObservableProperty]
    public partial string InningText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsHandicap { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoHistory))]
    public partial bool HasHistory { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPace1))]
    public partial bool HasPace1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPace2))]
    public partial bool HasPace2 { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<MatchHistoryRow> HistoryRows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public bool HasNoHistory => !HasHistory;

    public bool HasNoPace1 => !HasPace1;

    public bool HasNoPace2 => !HasPace2;

    public bool HasError => ErrorMessage.Length > 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("matchId", out var value) && int.TryParse(value?.ToString(), out var id))
        {
            _matchId = id;
            _ = LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var detail = await api.GetAsync<MatchStatDetail>($"api/mobile/stats/matches/{_matchId}");
            Apply(detail);
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

    private void Apply(MatchStatDetail d)
    {
        Player1Name = d.Player1.Name;
        Player2Name = d.Player2.Name;
        Player1Score = d.Player1.Score;
        Player2Score = d.Player2.Score;
        Player1ScoreColor = d.Winner == 1 ? _winnerColor : _loserColor;
        Player2ScoreColor = d.Winner == 2 ? _winnerColor : _loserColor;
        Player1Summary = Summary(d.Player1);
        Player2Summary = Summary(d.Player2);
        DateText = d.PlayedAt.ToLocalTime().ToString("dd.MM.yyyy");
        TimeText = d.StartedAt is { } s && d.EndedAt is { } e
            ? $"{s.ToLocalTime():HH:mm} - {e.ToLocalTime():HH:mm}"
            : d.StartedAt is { } only ? $"{only.ToLocalTime():HH:mm}" : "";
        TableText = d.TableNo is { } t ? string.Format(L["Match_Table"], t) : "";
        InningText = $"{L["Match_Inning"]}: {d.Inning}";
        IsHandicap = d.IsHandicap;
        TargetText = d.IsHandicap
            ? $"{L["Match_Handicap"]}: {d.Player1.Target?.ToString() ?? "-"} - {d.Player2.Target?.ToString() ?? "-"}"
            : d.MatchTarget is { } mt ? $"{L["Match_Target"]}: {mt}" : "";

        var history = d.History ?? [];
        HistoryRows = BuildRows(history);
        HasHistory = HistoryRows.Count > 0;

        var innings = history.Select(h => h.Inning).Distinct().Order().ToList();
        Player1InningsDrawable.Values = InningValues(history, 1, innings);
        Player2InningsDrawable.Values = InningValues(history, 2, innings);

        var bucketMinutes = d.BucketMinutes > 0 ? d.BucketMinutes : 5;
        var buckets1 = d.Player1.PaceBuckets ?? [];
        var buckets2 = d.Player2.PaceBuckets ?? [];
        Player1PaceDrawable.BucketMinutes = bucketMinutes;
        Player2PaceDrawable.BucketMinutes = bucketMinutes;
        Player1PaceDrawable.BucketPoints = buckets1;
        Player2PaceDrawable.BucketPoints = buckets2;
        HasPace1 = buckets1.Count > 0;
        HasPace2 = buckets2.Count > 0;

        ChartUpdated?.Invoke();
    }

    private static string Summary(MatchSideDto side) =>
        $"{side.Average:F3} / {side.HighRun}";

    private static List<float> InningValues(List<MatchHistoryEntry> history, int slot, List<int> innings) =>
        innings.Select(i => (float)history.Where(h => h.Slot == slot && h.Inning == i).Sum(h => h.Score)).ToList();

    private static List<MatchHistoryRow> BuildRows(List<MatchHistoryEntry> history) =>
        history
            .GroupBy(h => h.Inning)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var p1 = g.Where(h => h.Slot == 1).OrderBy(h => h.TotalScore).LastOrDefault();
                var p2 = g.Where(h => h.Slot == 2).OrderBy(h => h.TotalScore).LastOrDefault();
                var time = g.Where(h => h.PlayedAt is not null).Select(h => h.PlayedAt!.Value).DefaultIfEmpty().Min();
                return new MatchHistoryRow(
                    g.Key,
                    time == default ? "" : time.ToLocalTime().ToString("HH:mm:ss"),
                    p1 is null ? "" : g.Where(h => h.Slot == 1).Sum(h => h.Score).ToString(),
                    p1 is null ? "" : p1.TotalScore.ToString(),
                    p2 is null ? "" : g.Where(h => h.Slot == 2).Sum(h => h.Score).ToString(),
                    p2 is null ? "" : p2.TotalScore.ToString());
            })
            .ToList();
}
