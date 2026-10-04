using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using BilliardIQ.Mobile.Services.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class GameListPageModel(GameRepository GameRepo, GameSyncService Sync) : BasePageModel
{

    [ObservableProperty]
    public partial IReadOnlyList<Game> Games { get; set; } = [];

    [ObservableProperty]
    public partial PlayerSummaryStats Stats { get; set; } = new();

    [RelayCommand]
    private async Task Appearing(string Limit)
    {
        await Sync.SyncPendingAsync();
        Games = await GameRepo.GetGamesAsync(int.Parse(Limit));
        Stats = await GameRepo.GetStatsAsync();
    }

    [RelayCommand]
    private async Task NavigateToGame(Game? game)
    {
        if (game is null)
        {
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
        if (game is not null && (await GameRepo.GetGameByIdAsync(game.Id))?.RemoteId is > 0 and var remoteId && !await Sync.DeleteRemoteAsync(remoteId))
        {
            await Shell.Current.DisplayAlertAsync(L["NewGame_DeleteFailed"], L["Auth_NetworkError"], "OK");
            return;
        }

        var isDeleted = await GameRepo.DeleteGame(game?.Id);
        if (isDeleted)
        {
            Games = await GameRepo.GetGamesAsync();
            Stats = await GameRepo.GetStatsAsync();
        }
    }
}
