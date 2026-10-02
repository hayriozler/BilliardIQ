namespace BilliardIQ.Mobile.Services;

public interface IUnityBridgeService
{
    void LaunchGame(string player1Name, string player2Name, int targetScore);
}
