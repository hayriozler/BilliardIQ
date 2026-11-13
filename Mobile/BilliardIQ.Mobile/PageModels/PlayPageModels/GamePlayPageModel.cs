using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.PlayPageModels;

public partial class GamePlayPageModel(IUnityBridgeService Bridge) : BasePageModel
{
    [RelayCommand]
    private void Play() => Bridge.LaunchGame(L["Game_Player1"], L["Game_Player2"], targetScore: 10);
}
