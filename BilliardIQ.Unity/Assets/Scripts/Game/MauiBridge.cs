using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Game
{
    // MauiBridge → Unity ile .NET MAUI uygulaması arasındaki haberleşme köprüsü.
    //
    // İki yönlü iletişim:
    //   MAUI → Unity : UnitySendMessage("MauiBridge", "StartGame", "") ile çağrılır.
    //                  MAUI, bu GameObject'i ismiyle ("MauiBridge") bulup metod çağırır.
    //   Unity → MAUI : Henüz aktif değil; ileride BroadcastReceiver veya JNI ile yapılacak.
    //
    // UaaL (Unity as a Library) mimarisi: Unity bir Activity olarak çalışır.
    // MAUI, Unity Activity'yi başlatır ve sonucunu StartActivityForResult ile alır.
    public class MauiBridge : MonoBehaviour
    {
        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Android geri butonunun uygulamayı kapatmasını engelle.
            // Varsayılanda geri butonu uygulamadan çıkar; biz bunu kendi kodumuzla yönetmek istiyoruz.
            Input.backButtonLeavesApp = false;
#endif
            // Bu GameObject'in ismi kesinlikle "MauiBridge" olmalı.
            // UnitySendMessage, GameObject'i ismiyle arar; farklı bir isim olursa mesaj kaybolur.
            gameObject.name = "MauiBridge";

            // Sahne BilliardSceneBuilder ile yeniden kurulmadıysa UIManager eksik olabilir.
            // Güvenlik önlemi: yoksa runtime'da oluştur.
            if (FindAnyObjectByType<UIManager>() == null)
                new GameObject("UIManager").AddComponent<UIManager>();
        }

        // Update() → her kare (frame) çalışır. Android geri butonu dinlenir.
        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Keyboard.current?.escapeKey → Android'de geri butonu, klavyede Escape tuşuna eşlenir.
            // wasPressedThisFrame → "bu kare mi basıldı?" sorusunu sorar; sürekli basılı tutma ile karışmaz.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                ExitToMaui();
#endif
        }

        // Unity'den MAUI uygulamasına geri döner.
        // UIManager'daki "BACK" butonu ve geri tuşu bu metodu çağırır.
        public static void ExitToMaui()
        {
            Debug.Log("[MAUI] ExitToMaui called");
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // AndroidJavaClass → Java sınıfına C#'tan erişim (JNI / Java Native Interface).
                // UnityPlayer.currentActivity → şu anda çalışan Android Activity nesnesi (BilliardUnityActivity).
                using var player   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");

                // activity.finish() → Android Activity'yi kapat.
                // MAUI, Unity Activity'yi StartActivityForResult ile başlattığından,
                // finish() çağrısı MAUI'ye geri döner ve OnActivityResult tetiklenir.
                activity.Call("finish");
                Debug.Log("[MAUI] activity.finish() called");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MAUI] ExitToMaui failed: {e.Message}");
            }
#else
            // Editor veya Windows'ta oyunu kapat (test için)
            Application.Quit();
#endif
        }

        // ── MAUI → Unity çağrıları ──────────────────────────────────────────────
        // Bu metodlar MAUI tarafından UnitySendMessage ile çağrılır.
        // UnitySendMessage her zaman bir string parametre gönderir (boş bile olsa).

        // MAUI "oyunu başlat" dediğinde çağrılır. GameController'a iletilir.
        public void StartGame()
        {
            if (GameController.Instance != null)
                GameController.Instance.StartGame();
        }

        // ── Unity → MAUI bildirimleri ───────────────────────────────────────────
        // Şu an sadece Debug.Log ile loglanıyor.
        // İleride MAUI tarafına gerçek mesaj gönderimi buraya eklenecek.

        // Beyaz top banta çarptığında GameController tarafından çağrılır.
        public static void SendCushionCount() => SendToMaui("OnCushionCount");

        // Tüm toplar durduğunda GameController tarafından çağrılır.
        public static void SendShotResult() => SendToMaui("OnShotResult");

        // Merkezi gönderim noktası. İleride UnitySendMessage çağrısı buraya gelecek.
        private static void SendToMaui(string method) =>
            Debug.Log($"[MauiBridge → MAUI] {method}");
    }
}
