using Android.Content;
using BilliardIQ.Mobile.Services;
using System.Text.Json;

namespace BilliardIQ.Mobile.Platforms.Android;

public class UnityBridgeService : IUnityBridgeService
{
    public const int RequestCodeUnity = 1001;

    public void LaunchGame(string player1Name, string player2Name, int targetScore)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No current Android activity.");

        var data = JsonSerializer.Serialize(new
        {
            player1Name,
            player2Name,
            targetScore
        });

        var intent = new Intent(activity, Java.Lang.Class.ForName("com.unity3d.player.UnityPlayerActivity"));
        intent.PutExtra("gameData", data);
        intent.AddFlags(ActivityFlags.ReorderToFront);
        activity.StartActivityForResult(intent, RequestCodeUnity);
    }
}
