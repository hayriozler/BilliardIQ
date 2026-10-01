using Android.App;
using Android.Content;
using Android.Content.PM;

namespace BilliardIQ.Mobile.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges =
        ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <summary>
    /// Called whenever MainActivity comes to the foreground.
    /// When Unity's BilliardUnityActivity calls moveTaskToBack(), Android
    /// brings the MAUI task to front and triggers this method.
    /// </summary>
    protected override void OnResume()
    {
        base.OnResume();

        if (UnityBridgeService.IsGameRunning)
        {
            UnityBridgeService.IsGameRunning = false;
            NavigateToHome();
        }
    }

    internal void NavigateToHome()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//home");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] NavigateToHome failed: {ex.Message}");
            }
        });
    }
}
