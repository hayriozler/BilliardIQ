using Android.Content;
using BilliardIQ.Mobile.Services;
using System.Text.Json;

namespace BilliardIQ.Mobile.Platforms.Android;

public class UnityBridgeService : IUnityBridgeService
{
    /// <summary>
    /// Set to true when Unity has been launched; cleared when MainActivity.OnResume()
    /// detects the return from the Unity task and navigates to the game list.
    /// </summary>
    public static bool IsGameRunning { get; set; }

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

        // BilliardUnityActivity (extends UnityPlayerActivity) runs in its own task
        // (launchMode="singleTask"). finish() is overridden to call moveTaskToBack()
        // so System.exit() is never triggered and MAUI returns cleanly to the foreground.
        //
        // FLAG_ACTIVITY_NEW_TASK  – ensures Unity gets its own task separate from MAUI.
        // FLAG_ACTIVITY_SINGLE_TOP – reuses the existing instance and calls onNewIntent()
        //                            with the fresh game data on subsequent launches.
        var intent = new Intent(activity, Java.Lang.Class.ForName("com.billiardiq.mobile.BilliardUnityActivity"));
        intent.PutExtra("gameData", data);
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);

        IsGameRunning = true;
        activity.StartActivity(intent);
    }
}
