using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Platforms.Windows;

public class UnityBridgeService(IAlertHandler AlertHandler) : IUnityBridgeService
{
    public void LaunchGame(string player1Name, string player2Name, int targetScore) =>    
        AlertHandler.ShowAlertAsync(
            "Platform_DoesNotSupport",
                "Platform_DoesNotSupport_Message",
                "Platform_DoesNotSupport_Ok");
  
}
