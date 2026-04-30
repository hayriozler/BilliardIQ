using BilliardIQ.Mobile.Services;
using CommunityToolkit.Mvvm.Input;

namespace BilliardIQ.Mobile.PageModels.PlayPageModels;

public partial class GamePlayPageModel : BasePageModel
{
    private readonly IUnityBridgeService _bridge;

    public GamePlayPageModel(IUnityBridgeService bridge)
    {
        _bridge = bridge;
    }

    [RelayCommand]
    private void Play()
    {
        // İleride oyuncu adları ve hedef skor NewGame'den alınacak
        _bridge.LaunchGame("Oyuncu 1", "Oyuncu 2", targetScore: 10);
    }
}
