using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Graphics;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using BilliardIQ.Mobile.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.PageModels.Stats;

public partial class PlayerStatsDetailPageModel : BasePageModel, IQueryAttributable
{
    private readonly ScoreboardPlayerSession _playerSession;
    private readonly MatchResultRepository _matchResultRepository;
    private readonly MatchScoreStatRepository _matchScoreStatRepository;

    public PlayerStatsDetailPageModel(ScoreboardPlayerSession playerSession, MatchResultRepository matchResultRepository, MatchScoreStatRepository matchScoreStatRepository)
    {
        _playerSession = playerSession;
        _matchResultRepository = matchResultRepository;
        _matchScoreStatRepository = matchScoreStatRepository;
    }

    public event Action? ChartUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial ScoreboardPlayer? Player { get; set; }

    public string DisplayName => Player is null
        ? string.Empty
        : string.IsNullOrWhiteSpace(Player.NickName) ? Player.Name : Player.NickName;

    public ObservableCollection<PlayerMatchEntry> Matches { get; } = [];

    public InningsChartDrawable ChartDrawable { get; } = new();
    public ScoringPaceChartDrawable ScoringPaceChartDrawable { get; } = new();

    [ObservableProperty]
    public partial bool HasScoringPace { get; set; }

    [ObservableProperty]
    public partial int GamesPlayed { get; set; }

    [ObservableProperty]
    public partial int Wins { get; set; }

    [ObservableProperty]
    public partial int Losses { get; set; }

    [ObservableProperty]
    public partial double AverageInnings { get; set; }

    [ObservableProperty]
    public partial int BestHighRun { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    public bool IsEmpty => !IsLoading && Matches.Count == 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("playerId", out var value)) return;
        if (value is not int playerId && !int.TryParse(value.ToString(), out playerId)) return;

        Player = _playerSession.FindById(playerId);
        LoadAsync(Player?.Id).FireAndForgetSafeAsync();
    }

    private async Task LoadAsync(int? playerId)
    {
        IsLoading = true;
        try
        {
            var results = await _matchResultRepository.GetAllAsync();
            var playerMatches = results
                .Where(r => r.InvolvesAsPlayer1(playerId) || r.InvolvesAsPlayer2(playerId))
                .OrderBy(r => r.PlayedAt)
                .ToList();

            Matches.Clear();
            foreach (var r in playerMatches)
            {
                var isPlayer1 = r.InvolvesAsPlayer1(playerId);
                Matches.Add(new PlayerMatchEntry
                {
                    PlayedAt = r.PlayedAt,
                    StartedAt = r.StartedAt,
                    EndedAt = r.EndedAt,
                    OpponentName = isPlayer1 ? r.Player2Name : r.Player1Name,
                    PlayerScore = isPlayer1 ? r.Player1Score : r.Player2Score,
                    OpponentScore = isPlayer1 ? r.Player2Score : r.Player1Score,
                    Inning = r.Inning,
                    HighRun = isPlayer1 ? r.Player1HighRun : r.Player2HighRun,
                    Average = isPlayer1 ? r.Player1Avg : r.Player2Avg,
                    Won = isPlayer1 ? r.Winner == 1 : r.Winner == 2,
                });
            }

            GamesPlayed = playerMatches.Count;
            Wins = Matches.Count(m => m.Won);
            Losses = GamesPlayed - Wins;
            AverageInnings = playerMatches.Count > 0 ? playerMatches.Average(r => (double)r.Inning) : 0;
            BestHighRun = Matches.Count > 0 ? Matches.Max(m => m.HighRun) : 0;

            ChartDrawable.Values = Matches.Select(m => (float)m.Inning).ToList();
            await LoadScoringPaceAsync(playerId, playerMatches.Count > 0 ? playerMatches[^1] : null);
            ChartUpdated?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Scoring pace shows the player's most recent match only — match_score_stat is bucketed
    // per match, so averaging across matches of different lengths isn't a like-for-like comparison.
    private async Task LoadScoringPaceAsync(int? playerId, MatchResult? latestMatch)
    {
        if (latestMatch is null)
        {
            HasScoringPace = false;
            return;
        }

        var slot = latestMatch.InvolvesAsPlayer1(playerId) ? 1 : 2;
        var stats = await _matchScoreStatRepository.GetByMatchResultIdAsync(latestMatch.Id);
        var slotStats = stats.Where(s => s.PlayerSlot == slot).ToList();
        if (slotStats.Count == 0)
        {
            HasScoringPace = false;
            return;
        }

        // Rows are sparse (no row for a bucket with zero points), so fill the gaps up to the
        // highest bucket index the match actually reported.
        var buckets = new int[slotStats.Max(s => s.BucketIndex) + 1];
        foreach (var stat in slotStats) buckets[stat.BucketIndex] += stat.TotalPoints;

        ScoringPaceChartDrawable.BucketPoints = buckets;
        ScoringPaceChartDrawable.BucketMinutes = latestMatch.ScoreDistributionBucketMinutes > 0 ? latestMatch.ScoreDistributionBucketMinutes : 5;
        HasScoringPace = true;
    }
}
