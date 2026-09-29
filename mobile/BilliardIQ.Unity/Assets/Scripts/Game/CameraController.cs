using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ISTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ISTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Assets.Scripts.Game
{
    // Kameranın hangi ön tanımlı açıda olduğunu takip eder.
    // UIManager butonuyla döngüsel olarak geçiş yapılır: 3D → Üst → Yan → 3D ...
    public enum CameraPreset { Angled, Top, Side }

    // CameraController → kamerayı "Target" noktası etrafında döndürür ve yakınlaştırır.
    // Küresel koordinat sistemi kullanır: Yaw (yatay döndürme) + Pitch (dikey açı) + Distance (mesafe).
    // Editörde fare sağ tık + kaydırma tekerleği, cihazda iki parmak ile kontrol edilir.
    public class CameraController : MonoBehaviour
    {
        [Header("Orbit")]
        public Transform Target;         // Kameranın etrafında döneceği nokta (masa merkezi)
        public float OrbitSpeed    = 0.2f; // İki parmak kaydırma hassasiyeti
        public float MinPolarAngle = -89f; // Kamera masanın altına giremez
        public float MaxPolarAngle =  89f; // Kamera masanın üstünden bakabilir

        [Header("Zoom")]
        public float MinDistance = 2f;   // Kameraya en fazla bu kadar yaklaşılabilir
        public float MaxDistance = 10f;  // Kamera bu kadar uzaklaşabilir
        public float ZoomSpeed   = 0.05f; // Pinch zoom hassasiyeti

        // Kameranın anlık açı ve mesafe değerleri.
        // _yaw → yatay döndürme (360° serbest)
        // _pitch → dikey açı (yukarıdan aşağıya)
        // _distance → Target noktasına uzaklık
        private float _yaw      =  0f;
        private float _pitch    = 50f;
        private float _distance =  8f;

        // İki parmak hareketini takip etmek için önceki kare değerleri.
        private Vector2 _lastMidPoint;
        private float   _lastPinchDist;
        private bool    _twoFingerActive; // İki parmak yeni mi başladı?

        // Devam eden yumuşak geçiş coroutine'i. Yeni preset seçilirse önce bu durdurulur.
        private Coroutine _transitionCo;

        // Start() → sahne yüklendikten sonra bir kez çalışır.
        void Start()
        {
            // EnhancedTouchSupport → Unity Input System'in gelişmiş dokunmatik API'sini etkinleştir.
            // Bunu çağırmadan ISTouch.activeTouches her zaman boş döner.
            EnhancedTouchSupport.Enable();

            // Target atanmamışsa sahneye boş bir referans noktası oluştur
            if (Target == null)
                Target = new GameObject("CameraTarget").transform;

            // Başlangıç kamera konumunu uygula
            ApplyTransform();
        }

        // LateUpdate() → her kare, tüm Update() çağrılarından sonra çalışır.
        // Kamerayı en son güncellemek için LateUpdate kullanılır:
        // Tüm objeler hareket ettikten sonra kamera konumlanır → kayma olmaz.
        void LateUpdate()
        {
            // Geçiş animasyonu sürüyorsa kullanıcı girişini yoksay; coroutine kamerayı kendisi güncelliyor.
            if (_transitionCo != null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouse(); // PC/Editor'da fare ile kontrol
#else
            HandleTouch(); // Telefonda iki parmakla kontrol
#endif
            ApplyTransform(); // Hesaplanan açıları kamera konumuna uygula
        }

        // ── Ön tanımlı kamera açıları ─────────────────────────────────────────

        // 3D genel görünüm: masaya 50° yukarıdan bakan varsayılan kamera
        public void GoToAngled() => SwitchPreset(pitch: 50f, yaw: 0f, dist: 8f);

        // Kuşbakışı: masaya tam üstten bakan görünüm
        public void GoToTop()    => SwitchPreset(pitch: 88f, yaw: 0f, dist: 7f);

        // Yan görünüm: masanın yanından, neredeyse masa seviyesinden
        public void GoToSide()   => SwitchPreset(pitch: 10f, yaw: 90f, dist: 9f);

        // Yumuşak geçiş başlatır. Eğer başka bir geçiş sürüyorsa onu iptal edip yenisini başlatır.
        private void SwitchPreset(float pitch, float yaw, float dist)
        {
            if (_transitionCo != null) StopCoroutine(_transitionCo);
            _transitionCo = StartCoroutine(SmoothTransition(pitch, yaw, dist));
        }

        // Coroutine: 0.7 saniyede mevcut açılardan hedef açılara yumuşakça geçer.
        // Mathf.SmoothStep → sinüs eğrisi gibi yavaşlar-hızlanır-yavaşlar (ease in-out).
        // try-finally → coroutine dışarıdan durdurulsa bile _transitionCo null'a sıfırlanır.
        private IEnumerator SmoothTransition(float tPitch, float tYaw, float tDist)
        {
            float duration = 0.7f;
            float elapsed  = 0f;
            float sPitch   = _pitch;    // başlangıç pitch
            float sYaw     = _yaw;      // başlangıç yaw
            float sDist    = _distance; // başlangıç mesafe

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    // SmoothStep: 0→1 arasında S-eğrisi interpolasyon
                    float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    _pitch    = Mathf.Lerp(sPitch, tPitch, t); // Lerp: doğrusal interpolasyon
                    _yaw      = Mathf.Lerp(sYaw,   tYaw,   t);
                    _distance = Mathf.Lerp(sDist,  tDist,  t);
                    ApplyTransform();
                    yield return null; // bir sonraki kareye kadar bekle
                }
                // Animasyon bitince kesin değerlere atla (float hataları birikmesin)
                _pitch = tPitch; _yaw = tYaw; _distance = tDist;
                ApplyTransform();
            }
            finally
            {
                // Coroutine normal biterse veya StopCoroutine ile durdurulursa burası çalışır.
                // _transitionCo null olmadan LateUpdate kullanıcı girişini dinlemeye başlamaz.
                _transitionCo = null;
            }
        }

        // ── Giriş işleyicileri ────────────────────────────────────────────────

        // PC/Editor için: sağ fare tuşu basılı + sürükleme = orbit, tekerlek = zoom
        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.isPressed)
            {
                // mouse.delta → bu kare fare ne kadar hareket etti (piksel)
                var delta = mouse.delta.ReadValue();
                _yaw   += delta.x * 0.2f;
                _pitch -= delta.y * 0.2f;
                // Kameranın ters dönmesini engelle
                _pitch  = Mathf.Clamp(_pitch, MinPolarAngle, MaxPolarAngle);
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.5f)
                _distance = Mathf.Clamp(_distance - scroll * 0.003f, MinDistance, MaxDistance);
        }

        // Dokunmatik ekran için: iki parmak arasındaki nokta = orbit, parmak aralığı = zoom
        private void HandleTouch()
        {
            var touches = ISTouch.activeTouches;
            // İki parmak yoksa orbit/zoom modu aktif değil; sıfırla
            if (touches.Count != 2) { _twoFingerActive = false; return; }

            var t0   = touches[0];
            var t1   = touches[1];
            var pos0 = t0.screenPosition;
            var pos1 = t1.screenPosition;

            Vector2 mid   = (pos0 + pos1) * 0.5f; // iki parmağın orta noktası
            float   pinch = Vector2.Distance(pos0, pos1); // parmaklar arası mesafe

            // İlk kare veya yeni parmak dokunuşu: referans noktasını kaydet, bu karede hareketi hesaplama
            if (!_twoFingerActive
                || t0.phase == ISTouchPhase.Began
                || t1.phase == ISTouchPhase.Began)
            {
                _lastMidPoint    = mid;
                _lastPinchDist   = pinch;
                _twoFingerActive = true;
                return;
            }

            // Orta nokta ne kadar kaydı? → orbit için kullan
            var midDelta = mid - _lastMidPoint;
            _yaw   += midDelta.x * OrbitSpeed;
            _pitch -= midDelta.y * OrbitSpeed;
            _pitch  = Mathf.Clamp(_pitch, MinPolarAngle, MaxPolarAngle);

            // Parmak aralığı değişimi → zoom için kullan (açıldı = yaklaş, kapandı = uzaklaş)
            float pinchDelta = (_lastPinchDist - pinch) * ZoomSpeed;
            _distance = Mathf.Clamp(_distance + pinchDelta, MinDistance, MaxDistance);

            _lastMidPoint  = mid;
            _lastPinchDist = pinch;
        }

        // Küresel koordinatları (yaw, pitch, distance) gerçek 3D kamera konumuna çevirir.
        // Quaternion.Euler → üç eksen açısından döndürme matrisi oluşturur.
        // transform.LookAt → kameranın her zaman Target noktasına bakmasını sağlar.
        private void ApplyTransform()
        {
            if (Target == null) return;
            // Pitch + Yaw açısından bir döndürme matrisi oluştur
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            // Kamerayı Target'tan "distance" kadar geride (z ekseninde) konumlandır
            transform.position = Target.position + rot * new Vector3(0f, 0f, -_distance);
            // Her zaman Target'a bak
            transform.LookAt(Target.position);
        }
    }
}
