using UIKit;
using System.Text.Json;
using BilliardIQ.Mobile.Services;

namespace BilliardIQ.Mobile.Platforms.iOS;

// iOS UaaL entegrasyonu için:
// Unity → File → Build Settings → iOS → Export → Xcode projesi oluştur
// UnityFramework.xcframework'ü MAUI iOS projesine ekle
// Aşağıdaki LaunchGame, UnityFramework üzerinden sahneyi başlatacak şekilde güncellenecek
public class UnityBridgeService : IUnityBridgeService
{
    public void LaunchGame(string player1Name, string player2Name, int targetScore)
    {
        var rootVc = UIApplication.SharedApplication.KeyWindow?.RootViewController
            ?? throw new InvalidOperationException("No root view controller.");

        var data = JsonSerializer.Serialize(new { player1Name, player2Name, targetScore });

        // TODO: UnityFramework yüklendiğinde burası şu şekilde güncellenecek:
        // var framework = UnityFramework.GetInstance();
        // framework.SetDataBundleId("com.unity3d.framework");
        // framework.RunEmbeddedWithArgc(0, null, null);
        // framework.SendMessageToGO("MauiBridge", "StartGame", data);
        //
        // Şimdilik placeholder alert göster
        var alert = UIAlertController.Create(
            "iOS - Yakında",
            "UnityFramework entegrasyonu tamamlandığında aktif olacak.",
            UIAlertControllerStyle.Alert);
        alert.AddAction(UIAlertAction.Create("Tamam", UIAlertActionStyle.Default, null));
        rootVc.PresentViewController(alert, true, null);
    }
}
