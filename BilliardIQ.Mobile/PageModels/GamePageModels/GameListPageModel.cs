using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.GamePageModels;

public partial class GameListPageModel(GameRepository GameRepo) : BasePageModel
{
    [ObservableProperty]
    public partial IReadOnlyList<Game> Games { get; set; } = [];
    [RelayCommand]
    private async Task Appearing(string Limit) => Games = await GameRepo.GetGamesAsync(int.Parse(Limit));

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
        var isDeleted = await GameRepo.DeleteGame(game?.Id);
        if (isDeleted)
        {
            Games = await GameRepo.GetGamesAsync();
            OnPropertyChanged(nameof(Games));
        }
    }
}
