using BilliardIQ.Game;
using UnityEngine;

namespace Assets.Scripts.Game
{
    // Oyunun hangi aşamada olduğunu takip eden durum makinesi.
    // WaitingForShot → oyuncu atış yapabilir
    // BallsMoving    → toplar hareket ediyor, atış yapılamaz
    // ShotResult     → atış sonucu değerlendiriliyor (çok kısa sürer)
    public enum GameState { WaitingForShot, BallsMoving, ShotResult }

    // GameController → oyunun akışını yöneten ana sınıf.
    // Singleton pattern: Instance ile sahnedeki tek örneğe erişilir.
    // Hangi durumda olduğumuzu (bekleme, hareket, sonuç) takip eder.
    public class GameController : MonoBehaviour
    {
        // Sahnedeki tek GameController örneğine static erişim.
        // Diğer scriptler GameController.Instance.OnShotFired() şeklinde kullanır.
        public static GameController Instance { get; private set; }

        [Header("Rules")]
        public int targetScore = 10; // Kazanmak için gereken puan (henüz UI'a bağlı değil)
        public GameState State { get; private set; } = GameState.WaitingForShot;

        // Awake() → sahne yüklendiğinde en erken çağrılır.
        // Singleton instance burada kurulur: bu nesne = GameController.Instance
        void Awake()
        {
            Instance = this;

            // HitPointSelector bileşeni sahnede yoksa runtime'da oluştur.
            // Normalde BilliardSceneBuilder tarafından eklenir; güvenlik önlemi olarak burada da kontrol edilir.
            if (FindAnyObjectByType<HitPointSelector>() == null)
                new GameObject("HitPointSelector").AddComponent<HitPointSelector>();
        }

        // Atış tetiklendiğinde CueController tarafından çağrılır.
        // Durum BallsMoving'e geçer → oyuncu yeni atış yapamaz.
        public void OnShotFired() => State = GameState.BallsMoving;

        // Beyaz top banta çarptığında BallController tarafından çağrılır.
        // MauiBridge üzerinden MAUI uygulamasına bant sayısını bildirir.
        public void RegisterCushionHit() => MauiBridge.SendCushionCount();

        // Tüm toplar durduğunda TableController tarafından çağrılır.
        // Atış sonucunu MAUI'ye bildirir ve bir sonraki atış için sahneyi hazırlar.
        public void OnBallsStopped()
        {
            State = GameState.ShotResult;
            MauiBridge.SendShotResult(); // MAUI'ye "atış bitti" mesajı gönder
            State = GameState.WaitingForShot;
            TableController.Instance.ResetForNextShot(); // ıstakayı tekrar aktifleştir
        }

        // MAUI uygulaması "oyunu başlat" dediğinde MauiBridge üzerinden çağrılır.
        // Topları başlangıç konumlarına sıfırlar ve atışa hazır hale getirir.
        public void StartGame()
        {
            State = GameState.WaitingForShot;
            TableController.Instance.StartNewGame();
        }
    }
}
