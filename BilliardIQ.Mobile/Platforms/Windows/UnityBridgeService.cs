using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Services;

// Windows'ta Unity oyunu desteklenmiyor — stub implementasyon
public class UnityBridgeService : IUnityBridgeService
{
    public void LaunchGame(string player1Name, string player2Name, int targetScore)
    {
        Application.Current!.MainPage!.DisplayAlert(
            "Platform Desteklenmiyor",
            "3-Bant oyunu yalnızca Android'de çalışır.",
            "Tamam");
    }
}
