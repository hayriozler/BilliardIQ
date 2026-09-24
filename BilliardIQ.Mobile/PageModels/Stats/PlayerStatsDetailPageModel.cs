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

    public PlayerStatsDetailPageModel(ScoreboardPlayerSession playerSession, MatchResultRepository matchResultRepository)
    {
        _playerSession = playerSession;
        _matchResultRepository = matchResultRepository;
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
        LoadAsync(playerId).FireAndForgetSafeAsync();
    }

    private async Task LoadAsync(int playerId)
    {
        IsLoading = true;
        try
        {
            var results = await _matchResultRepository.GetAllAsync();
            var playerMatches = results
                .Where(r => r.Player1Id == playerId || r.Player2Id == playerId)
                .OrderBy(r => r.PlayedAt)
                .ToList();

            Matches.Clear();
            foreach (var r in playerMatches)
            {
                var isPlayer1 = r.Player1Id == playerId;
                Matches.Add(new PlayerMatchEntry
                {
                    PlayedAt = r.PlayedAt,
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
            ChartUpdated?.Invoke();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
