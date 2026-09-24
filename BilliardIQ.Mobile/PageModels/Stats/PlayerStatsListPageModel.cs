using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BilliardIQ.Mobile.PageModels.Stats;

public partial class PlayerStatsListPageModel : BasePageModel
{
    private readonly ScoreboardPlayerSession _playerSession;
    private readonly MatchResultRepository _matchResultRepository;

    public PlayerStatsListPageModel(ScoreboardPlayerSession playerSession, MatchResultRepository matchResultRepository)
    {
        _playerSession = playerSession;
        _matchResultRepository = matchResultRepository;
    }

    public ObservableCollection<PlayerMatchStats> Stats { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    public bool IsEmpty => !IsLoading && Stats.Count == 0;

    [RelayCommand]
    private async Task Appearing()
    {
        IsLoading = true;
        try
        {
            var results = await _matchResultRepository.GetAllAsync();

            var summaries = _playerSession.Players
                .Select(player => BuildStats(player, results))
                .OrderByDescending(s => s.Wins)
                .ThenByDescending(s => s.GamesPlayed)
                .ToList();

            // Update in place (Replace/Add/RemoveAt) instead of Clear()+Add — Clear() raises a
            // Reset notification that forces the CollectionView's Android adapter to rebind every
            // visible row in one bulk pass, which crashes the RelativeSource-ancestor binding in
            // the item template while rows are mid-recycle.
            for (var i = 0; i < summaries.Count; i++)
            {
                if (i < Stats.Count) Stats[i] = summaries[i];
                else Stats.Add(summaries[i]);
            }
            for (var i = Stats.Count - 1; i >= summaries.Count; i--) Stats.RemoveAt(i);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static PlayerMatchStats BuildStats(ScoreboardPlayer player, IReadOnlyList<MatchResult> results)
    {
        var matches = results.Where(r => r.Player1Id == player.Id || r.Player2Id == player.Id).ToList();
        var wins = matches.Count(r => (r.Player1Id == player.Id && r.Winner == 1) || (r.Player2Id == player.Id && r.Winner == 2));

        return new PlayerMatchStats
        {
            Player = player,
            GamesPlayed = matches.Count,
            Wins = wins,
            Losses = matches.Count - wins,
            AverageInnings = matches.Count > 0 ? matches.Average(r => (double)r.Inning) : 0,
            AveragePerInning = matches.Count > 0 ? matches.Average(r => r.Player1Id == player.Id ? r.Player1Avg : r.Player2Avg) : 0,
            BestHighRun = matches.Count > 0 ? matches.Max(r => r.Player1Id == player.Id ? r.Player1HighRun : r.Player2HighRun) : 0,
        };
    }

    [RelayCommand]
    private async Task SelectPlayer(PlayerMatchStats stats) =>
        await Shell.Current.GoToAsync($"playerstatsdetail?playerId={stats.Player.Id}");
}
