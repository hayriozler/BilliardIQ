using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Pages.Analyzers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class GameListPageModel(GameRepository gameRepo, IServiceProvider services) : BasePageModel
{
    private readonly GameRepository _gameRepo = gameRepo;
    private readonly IServiceProvider _services = services;

    [ObservableProperty]
    public partial IReadOnlyList<Game> Games { get; set; } = [];

    [ObservableProperty]
    public partial PlayerSummaryStats Stats { get; set; } = new();

    [RelayCommand]
    private async Task Appearing(string Limit)
    {
        Games = await _gameRepo.GetGamesAsync(int.Parse(Limit));
        Stats = await _gameRepo.GetStatsAsync();
    }

    [RelayCommand]
    private static async Task NavigateToGame(Game? game)
    {
        if (game is null)
            await Shell.Current.GoToAsync($"newgame");
        else
            await Shell.Current.GoToAsync($"newgame", new Dictionary<string, object>
            {
                { "gameId", game.Id }
            });
    }

    [RelayCommand]
    private async Task Delete(Game? game)
    {
        var isDeleted = await _gameRepo.DeleteGame(game?.Id);
        if (isDeleted)
        {
            Games = await _gameRepo.GetGamesAsync();
            Stats = await _gameRepo.GetStatsAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToPhoto()
    {
#if ANDROID
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
#endif
        var page = _services.GetRequiredService<PhotoAnalyzerViewPage>();
        await Shell.Current.Navigation.PushModalAsync(page, animated: false);
    }
}
