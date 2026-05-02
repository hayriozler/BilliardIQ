using BilliardIQ.Mobile.Data;
using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.PlayPageModels;

public partial class GamePlayPageModel(IUnityBridgeService Bridge, IAlertHandler AlertHandler, PlayerRepository PlayerRepo) : BasePageModel
{
    [RelayCommand]
    private async Task Play()
    {
        var player = await PlayerRepo.GetPlayerAsync();
        if (player is null)
        {
            await AlertHandler.ShowAlertAsync(
                "NewGame_NoProfile_Title",
                "NewGame_NoProfile_Message",
                "NewGame_NoProfile_Ok");
            await Shell.Current.GoToAsync("//profile");
            return;
        }

        Bridge.LaunchGame(L["Game_Player1"], L["Game_Player2"], targetScore: 10);
    }
}
