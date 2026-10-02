using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Pages.Analyzers;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class GameListPageModel(GameRepository GameRepo, PlayerRepository PlayerRepo, IAlertHandler AlertHandler, IServiceProvider Services) : BasePageModel
{

    [ObservableProperty]
    public partial IReadOnlyList<Game> Games { get; set; } = [];

    [ObservableProperty]
    public partial PlayerSummaryStats Stats { get; set; } = new();

    [RelayCommand]
    private async Task Appearing(string Limit)
    {
        Games = await GameRepo.GetGamesAsync(int.Parse(Limit));
        Stats = await GameRepo.GetStatsAsync();
    }

    [RelayCommand]
    private async Task NavigateToGame(Game? game)
    {
        if (game is null)
        {
            var player = await PlayerRepo.GetPlayerAsync();
            if (player is null)
            {
                await AlertHandler.ShowAlertAsync("NewGame_NoProfile_Title",
                    "NewGame_NoProfile_Message",
                    "NewGame_NoProfile_Ok");
                await Shell.Current.GoToAsync("//profile");
                return;
            }
            await Shell.Current.GoToAsync("newgame");
        }
        else
        {
            await Shell.Current.GoToAsync("newgame", new Dictionary<string, object>
            {
                { "gameId", game.Id }
            });
        }
    }

    [RelayCommand]
    private async Task Delete(Game? game)
    {
        var isDeleted = await GameRepo.DeleteGame(game?.Id);
        if (isDeleted)
        {
            Games = await GameRepo.GetGamesAsync();
            Stats = await GameRepo.GetStatsAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToPhoto()
    {
#if ANDROID
        ((Android.App.Activity)Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!)
            .RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
#endif
        var page = Services.GetRequiredService<PhotoAnalyzerViewPage>();
        await Shell.Current.Navigation.PushModalAsync(page, animated: false);
    }
}
