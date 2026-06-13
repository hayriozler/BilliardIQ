using Assets.Scripts.Game;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BilliardIQ.Game
{
    // TableController → masayı ve topları yöneten koordinatör.
    // Atış sonrası topları izler, durduktan sonra GameController'ı haberdar eder.
    // Singleton: TableController.Instance ile her yerden erişilir.
    public class TableController : MonoBehaviour
    {
        public static TableController Instance { get; private set; }

        // Unity Inspector'dan sürükle-bırak ile atanan top referansları.
        // [Header] → Inspector'da başlık gösterir, bölümleme için kullanılır.
        [Header("Balls")]
        public BallController cueBall;
        public BallController yellowBall;
        public BallController redBall;

        // Her topun oyun başında hangi konumda olduğunu tutan Transform referansları.
        // Bunlar sahneye "StartCueBall" gibi boş GameObject'ler olarak yerleştirilir.
        [Header("Start Positions")]
        public Transform cueBallStart;
        public Transform yellowBallStart;
        public Transform redBallStart;

        // Tüm topları döngüde işlemek için dizide tutuyoruz.
        // null olan topları hariç tutar (LINQ .Where ile filtreleme).
        private BallController[] _allBalls;

        // WaitForBallsToStop coroutine referansı.
        // Yeni bir atış başlamadan önce önceki coroutine'i durdurabilmek için saklanır.
        private Coroutine _stopCheckCoroutine;

        // Awake() → sahnedeki en erken başlatma.
        // Singleton ve top dizisi burada kurulur.
        void Awake()
        {
            Instance = this;
            // null olmayan topları diziye topla. Böylece foreach döngülerinde null kontrolü yapmak zorunda kalmayız.
            _allBalls = new[] { cueBall, yellowBall, redBall }.Where(b => b != null).ToArray();
        }

        // Start() → Awake()'ten sonra, sahne tamamen yüklendikten sonra çalışır.
        // Bağımlı scriptlere (CueController, HitPointSelector) referanslar burada verilir
        // çünkü onların Awake()'i çoktan çalışmış, Instance'ları hazırdır.
        void Start()
        {
            // DiamondMarkers bileşenini runtime'da bu objeye ekle.
            // Editörde değil, çalışma zamanında eklenir çünkü masa ölçüleri sabit ve tünel etkisi yok.
            gameObject.AddComponent<DiamondMarkers>();

            Debug.Log($"[TC] Start — cueBall={cueBall}, cueBallStart={cueBallStart}, yellow={yellowBall}, red={redBall}");

            // HitPointSelector'ü beyaz top ile ilişkilendir (spin bilgisi için).
            if (HitPointSelector.Instance != null)
                HitPointSelector.Instance.Init(cueBall);

            // Topları başlangıç konumlarına taşı
            ResetBallPositions();

            // ThicknessIndicator'ün hangi topların sarı ve kırmızı olduğunu bilmesi gerekiyor.
            if(ThicknessIndicator.Instance != null)
            ThicknessIndicator.Instance.SetBalls(yellowBall, redBall);

            // Oyun başladığında ıstakayı etkinleştir ve beyaz topa yönelt.
            if (CueController.Instance != null)
            {
                CueController.Instance.Enable(cueBall);
                Debug.Log("[TC] CueController.Enable called");
            }
            else
                Debug.LogError("[TC] CueController.Instance is null — cue will not show");
        }

        // Bir atıştan sonra, aynı oyun devam ederken çağrılır.
        // Toplar kaldığı yerde durur (başlangıca dönmez), ıstaka tekrar aktif olur.
        public void ResetForNextShot()
        {
            // Hâlâ devam eden bir "toplar durdu mu?" kontrolü varsa durdur
            if (_stopCheckCoroutine != null)
                StopCoroutine(_stopCheckCoroutine);

            // Tüm topları durdur ve atış bayrağını kapat
            foreach (var b in _allBalls)
                if (b != null) { b.Stop(); b.SetShotActive(false); }

            // Istakayı tekrar etkinleştir, oyuncu atış yapabilsin
            if (CueController.Instance != null)
                CueController.Instance.Enable(cueBall);
        }

        // Yeni bir oyun başlarken çağrılır. Topları başlangıç konumlarına sıfırlar.
        // ResetForNextShot'tan farkı: toplar hareket etmez, başa döner.
        public void StartNewGame()
        {
            if (_stopCheckCoroutine != null)
                StopCoroutine(_stopCheckCoroutine);

            foreach (var b in _allBalls)
                if (b != null) { b.Stop(); b.SetShotActive(false); }

            ResetBallPositions(); // ← fark: toplar başlangıç konumuna döner

            if (CueController.Instance != null)
                CueController.Instance.Enable(cueBall);
        }

        // Atış tetiklendiğinde CueController → GameController → burası zinciriyle çağrılır.
        // Tüm toplarda "atış aktif" bayrağını açar ve topların durmasını beklemeye başlar.
        public void OnShotFired()
        {
            foreach (var b in _allBalls)
                b.SetShotActive(true);

            // Önceki coroutine'i temizle ve yenisini başlat
            if (_stopCheckCoroutine != null)
                StopCoroutine(_stopCheckCoroutine);

            _stopCheckCoroutine = StartCoroutine(WaitForBallsToStop());
        }

        // Tüm topları başlangıç pozisyonlarına taşır.
        private void ResetBallPositions()
        {
            ResetBall(cueBall,    cueBallStart);
            ResetBall(yellowBall, yellowBallStart);
            ResetBall(redBall,    redBallStart);
        }

        // Tek bir topu başlangıç pozisyonuna taşır ve durdurur.
        private static void ResetBall(BallController ball, Transform startPos)
        {
            if (ball == null) return;
            ball.Stop();
            // startPos null olabilir (Inspector'da referans atanmamışsa); bu durumda pozisyonu değiştirme.
            if (startPos != null)
                ball.transform.position = startPos.position;
        }

        // Coroutine: topların tamamen durmasını bekler, sonra GameController'a haber verir.
        // IEnumerator → Unity'de "coroutine" yazmak için kullanılır.
        // yield return → "bu noktada dur, bir sonraki frame'de devam et" anlamına gelir.
        private IEnumerator WaitForBallsToStop()
        {
            // Topların hızlanması için 0.5 saniye bekle; hemen kontrol etmek "zaten durdu" diyebilir.
            yield return new WaitForSeconds(0.5f);

            float elapsed = 0f;
            const float maxWait = 12f; // Toplar 12 saniyede durmadıysa zorla durdur (sonsuz döngü önlemi)

            while (elapsed < maxWait)
            {
                bool anyMoving = false;
                foreach (var b in _allBalls)
                {
                    if (b != null && b.IsMoving()) { anyMoving = true; break; }
                }

                if (!anyMoving) break; // Tüm toplar durdu → döngüden çık

                // 0.1 saniye bekle, sonra tekrar kontrol et.
                // yield return WaitForSeconds → bu süre boyunca diğer kodlar çalışmaya devam eder.
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            // Hâlâ hareket eden top varsa zorla durdur (zaman aşımı)
            foreach (var b in _allBalls)
                if (b != null) b.Stop();

            // GameController'a "toplar durdu, atışı değerlendir" sinyali gönder
            if (GameController.Instance != null)
                GameController.Instance.OnBallsStopped();
        }
    }
}
