using BilliardIQ.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ISTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ISTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Assets.Scripts.Game
{
    // CueController → oyuncunun ıstakayı (cue stick) kontrol etmesini sağlar.
    //
    // Kontrol şeması (şu anki aktif mod):
    //   - Parmak X yönünde kayar → ıstaka döner (yaw/yön açısı değişir)
    //   - Parmak Y yönünde aşağı kayar → atış gücü artar (pullback)
    //   - Parmak ekrandan kalkar → atış yapılır
    //   - İkinci parmak basar → atıştan vazgeç (cancel)
    //   - Hızla yukarı swipe → atıştan vazgeç (cancel)
    //
    // Görsel:
    //   - Nişan çizgisi (AimLine): noktalı, ilk topa veya ilk banda kadar uzanır
    //   - Istaka çizgisi (CueLine): vurma anında ileri atlar, sonra geri çekilir
    public class CueController : MonoBehaviour
    {
        // Singleton: CueController.Instance ile TableController ve diğerlerinden erişilir.
        public static CueController Instance { get; private set; }

        [Header("Shot Settings")]
        // Maksimum atış kuvveti: mass=0.2f ile impulse/mass = hız (m/s).
        // 5f → tam güçte ~25 m/s (gerçek bilardoda max ~10 m/s; biraz abartılı ama oynanabilir).
        // 12f → ~60 m/s idi, gerçekçi değildi.
        public float MaxPower = 5f;
        public float MaxDragDistance = 1.5f;   // Slider tam doluyken bu kadar pullback hesaplanır

        [Header("Visuals")]
        public LineRenderer AimLine; // Inspector'dan atanır veya Awake'te oluşturulur

        // Oyun alanı sınırları: BallController._maxX/_maxZ ile eşleşmeli
        private const float _halfX = 2.72f;
        private const float _halfZ = 1.29f;

        // ── Durum değişkenleri ────────────────────────────────────────────────
        private BallController _cueBall;         // şu anda kontrol ettiğimiz beyaz top
        private Camera _cam;                     // ekran→dünya koordinat dönüşümü için
        private bool _enabled;                   // CueController aktif mi?
        private bool _dragging;                  // şu an sürükleme (atış hazırlığı) yapılıyor mu?

        // Nişan yönü (yatay düzlemde normalize edilmiş vektör)
        // _lastAimDir = _targetAimDir her kare: delta-tabanlı yaw zaten kademeli değişiyor,
        // RotateTowards gereksizdi ve 10x gecikmeye (8 derece/saniye limit) neden oluyordu.
        private Vector3 _lastAimDir = Vector3.forward;
        private Vector3 _targetAimDir = Vector3.forward;


        private float _yaw;                       // anlık yaw açısı (derece, Y ekseni etrafında dönüş)
        // Her piksel kaç derece döndürür. 0.2 → 500px tarama = 100° → ıstaka rahat dönüyor.
        public float RotationSensitivity = 0.2f;

        private LineRenderer _cueLine; // ıstaka çizgisi (görsel)

        // Awake() → sahne yüklenirken en erken çalışır.
        void Awake()
        {
            Instance = this;
            _cam     = Camera.main; // sahnenin ana kamerası
            Debug.Log($"[CUE] Awake — cam={_cam}");
            SetupAimLine();
            SetupCueLine();
            Debug.Log($"[CUE] Awake done — cueLine={_cueLine}, aimLine={AimLine}");
        }

        // Start() → EnhancedTouch dokunmatik sistemi burada etkinleştirilir.
        // Awake'te yapılmaz çünkü diğer scriptlerin de Awake'te başlatması gerekebilir.
        void Start() => EnhancedTouchSupport.Enable();

        // ── Görsel kurulum ────────────────────────────────────────────────────

        // Nişan çizgisini (AimLine) kurar: noktalı desen, dünya uzayında çizgi.
        private void SetupAimLine()
        {
            // Inspector'dan AimLine atanmadıysa runtime'da oluştur
            if (AimLine == null)
            {
                var go = new GameObject("AimLine");
                AimLine = go.AddComponent<LineRenderer>();
            }

            AimLine.positionCount = 2;      // her zaman 2 nokta: başlangıç ve bitiş
            AimLine.startWidth    = 0.015f; // nişanın başında geniş
            AimLine.endWidth      = 0.008f; // nişanın sonunda dar (perspektif hissi)
            AimLine.useWorldSpace = true;   // noktalar dünya koordinatlarında verilir (obje koordinatı değil)
            AimLine.enabled       = false;  // başta gizli

            // Noktalı desen için 2 piksellik texture: beyaz | şeffaf
            // wrapMode=Repeat ile bu desen çizgi boyunca tekrarlanır → noktalı görünüm
            var dashTex = new Texture2D(2, 1, TextureFormat.RGBA32, false);
            dashTex.SetPixel(0, 0, Color.white);
            dashTex.SetPixel(1, 0, Color.clear);
            dashTex.filterMode = FilterMode.Point;   // piksel kenarları keskin kalsın (bulanıklık olmasın)
            dashTex.wrapMode   = TextureWrapMode.Repeat; // çizgi boyunca tekrarla
            dashTex.Apply();

            // GameMaterials'tan asset materyali al (build güvenliği için); yoksa runtime'da oluştur
            var baseMat = GameMaterials.Instance != null ? GameMaterials.Instance.AimLine : null;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = baseMat != null ? new(baseMat) : new(shader);
            mat.mainTexture      = dashTex;
            mat.mainTextureScale = new Vector2(8f, 1f); // 8 = çizgi boyunca 8 tekrar → nokta sıklığı

            AimLine.material    = mat;
            AimLine.textureMode = LineTextureMode.Tile; // texture çizgi boyunca tile'lanır
        }

        // Istaka çizgisini (CueLine) kurar: kahverengi→açık renk geçişli kalın çizgi.
        private void SetupCueLine()
        {
            var go = new GameObject("CueLine");
            _cueLine = go.AddComponent<LineRenderer>();

            _cueLine.positionCount = 2;      // 2 nokta: ıstakanın ucu ve sapı
            _cueLine.startWidth    = 0.04f;  // uç: ince (vuruş ucu)
            _cueLine.endWidth      = 0.012f; // sap: daha ince (perspektif)
            _cueLine.useWorldSpace = true;
            _cueLine.enabled       = false;

            // Renk geçişi: koyu kahverengi (uç) → açık kahverengi (sap) → tahta görünümü
            _cueLine.startColor = new Color(0.45f, 0.28f, 0.10f, 1f);
            _cueLine.endColor   = new Color(0.80f, 0.60f, 0.30f, 1f);

            var instance = GameMaterials.Instance;
            var baseMat = instance != null ? instance.CueLine : null;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            _cueLine.material = baseMat != null ? baseMat : new(shader);
        }

        // ── Dış API ───────────────────────────────────────────────────────────

        // TableController atış hazırlığı için ıstakayı etkinleştirir.
        // cueBall = kontrol edilecek beyaz top
        public void Enable(BallController cueBall)
        {
            _cueBall         = cueBall;
            _enabled         = true;
            _dragging        = false;

            // Varsayılan nişan yönü: beyaz topun pozisyonundan masa merkezine doğru
            var p = cueBall.transform.position;
            _lastAimDir = new Vector3(-p.x, 0f, -p.z).normalized;

            // Yaw açısını mevcut nişan yönünden hesapla (Atan2 = açıya çevir)
            _yaw = Mathf.Atan2(_lastAimDir.x, _lastAimDir.z) * Mathf.Rad2Deg;

            if (_lastAimDir.sqrMagnitude < 0.01f)
                _lastAimDir = new Vector3(1f, 0f, 0f); // top masanın tam ortasındaysa yatay yön kullan

            _targetAimDir = _lastAimDir; // hedef = mevcut → başlangıçta tutarsız dönüş olmasın

            // Görseli hemen güncelle; bir sonraki Update'i bekleme
            UpdateVisuals();
        }

        // Istakayı devre dışı bırakır (atış başladığında veya sahne sıfırlanınca).
        public void Disable()
        {
            _enabled         = false;
            _dragging        = false;
            AimLine.enabled  = false;
            _cueLine.enabled = false;
        }

        // Atış iptal edildiğinde çağrılır: sürükleme biter.
        // Slider'a dokunulmaz; slider bağımsız kontrol edilir ve kendi gücünü korur.
        public void CancelDrag() => _dragging        = false;

        // ── Update döngüsü ────────────────────────────────────────────────────

        // Update() → her kare çalışır. Nişan yönünü yumuşatır, görseli günceller, girişi işler.
        void Update()
        {
            // CueController aktif değilse veya oyun bekleme durumunda değilse çizgileri gizle
            if (!_enabled || GameController.Instance == null || GameController.Instance.State != GameState.WaitingForShot)
            {
                AimLine.enabled  = false;
                _cueLine.enabled = false;
                return;
            }

            // _targetAimDir her kare delta.x * sensitivity kadar değişir (UpdateDrag içinde).
            // Bu küçük kademeli değişim zaten yumuşak bir dönüş hissi verir.
            // RotateTowards ile yapılan ek yumuşatma 8 derece/saniye ile sınırlıyordu
            // ve parmak hareketinin ~10 kat gerisinde kalıyordu → doğrudan atama yeterli.
            _lastAimDir = _targetAimDir;

            UpdateVisuals();

            // HitPointSelector açıksa (oyuncu spin seçiyor) ıstaka girişini durdur
            if (HitPointSelector.Instance != null && HitPointSelector.Instance.IsOpen) return;

            // Platform bağımlı giriş: PC'de fare, telefonda dokunmatik
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        // ── Görseller ─────────────────────────────────────────────────────────

        // Her kare çalışır: ıstaka çizgisini ve nişan çizgisini günceller.
        private void UpdateVisuals()
        {
            if (_cueLine == null || _cueBall == null) return;
            var ballPos = _cueBall.transform.position;
            var dir = _lastAimDir; // anlık (yumuşatılmış) nişan yönü

            // Pullback görsel konumu her zaman slider değerine göre hesaplanır.
            // Eski modelde sürükleme mesafesi pullback'i belirliyordu; artık slider belirliyor.
            // Böylece ıstaka, oyuncu yön değiştirirken bile slider gücünü doğru gösteriyor.
            float sliderPower = PowerSlider.Instance != null ? PowerSlider.Instance.Power : 0.25f;
            float pull = sliderPower * MaxDragDistance * 1.5f + 0.15f;

            // Istaka çizgisi: beyaz topun arkasında, nişan yönünün tersinde
            _cueLine.enabled = true;
            _cueLine.SetPosition(0, ballPos - dir * 0.12f);             // ucun birkaç cm gerisinde başlar
            _cueLine.SetPosition(1, ballPos - dir * (pull + 0.12f));    // pullback kadar geride biter

            // ThicknessIndicator'ü anlık nişan yönüne göre güncelle
            if (ThicknessIndicator.Instance != null)
                ThicknessIndicator.Instance.UpdateFromAim(ballPos, dir);

            // Nişan çizgisi noktalarını hesapla (topa veya banda kadar)
            var traj = CalculateTrajectory(ballPos, dir);
            AimLine.enabled       = true;
            AimLine.positionCount = traj.Count;

            // Sürükleme sırasında daha parlak, dinlenme sırasında daha soluk
            float alpha = _dragging ? 0.90f : 0.40f;
            SetTrajectoryGradient(alpha);

            // Nişan çizgisi noktalarını biraz yukarı kaldır (masa yüzeyinde kaybolmasın)
            for (int i = 0; i < traj.Count; i++)
                AimLine.SetPosition(i, traj[i] + Vector3.up * 0.02f);
        }

        // Nişan çizgisinin renk geçişini ayarlar: topun yakınında parlak, uzakta soluk.
        // Gradient → Unity'nin renk gradyan sınıfı; LineRenderer bunu pozisyon oranına göre uygular.
        private void SetTrajectoryGradient(float baseAlpha)
        {
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 1f, 0.6f), 0f), // başlangıç: sarımsı beyaz
                    new GradientColorKey(new Color(1f, 1f, 0.5f), 1f)  // son: biraz daha sarı
                },
                new[]
                {
                    new GradientAlphaKey(baseAlpha,           0f),   // başta: tam görünür
                    new GradientAlphaKey(baseAlpha * 0.5f,  0.4f),   // ortada: yarı saydam
                    new GradientAlphaKey(baseAlpha * 0.15f, 1.0f)    // sonda: neredeyse görünmez
                });
            AimLine.colorGradient = grad;
        }

        // ── Giriş işleme ──────────────────────────────────────────────────────

        // PC/Editor: sol fare tuşu ile sürükleme.
        private void HandleMouseInput()
        {
            if (IsOverUI()) return; // fare UI üzerindeyse oyun girişi yoksay
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame) BeginDrag(mouse.position.ReadValue());
            else if (mouse.leftButton.isPressed   && _dragging) UpdateDrag(mouse.position.ReadValue());
            else if (mouse.leftButton.wasReleasedThisFrame && _dragging) EndDrag();
        }

        // Telefon: tek parmak ile kontrol; iptal için ikinci parmak veya hızlı yukarı swipe.
        private void HandleTouchInput()
        {
            var touches = ISTouch.activeTouches;
            if (touches.Count == 0) return;

            // İptal yöntemi 1: Sürükleme sırasında ikinci parmak gelirse iptal et
            if (touches.Count >= 2 && _dragging)
            {
                CancelDrag();
                return;
            }

            var touch = touches[0];

            // İptal yöntemi 2: Hızla yukarı swipe
            // touch.delta.y / Time.deltaTime → piksel/saniye cinsinden yukarı hız
            // 2000 piksel/saniye ≈ ekranın üçte birini 0.15 saniyede geçmek
            if (_dragging && touch.phase == ISTouchPhase.Moved)
            {
                float upSpeed = touch.delta.y / Time.deltaTime;
                if (upSpeed > 2000f)
                {
                    CancelDrag();
                    return;
                }
            }

            // Dokunuş UI elemanının üzerinde başladıysa oyun girişi olarak işleme
            if (touch.phase == ISTouchPhase.Began && IsOverUI(touch.touchId)) return;
            switch (touch.phase)
            {
                case ISTouchPhase.Began:   BeginDrag(touch.screenPosition); break;
                case ISTouchPhase.Moved:   if (_dragging) UpdateDrag(touch.screenPosition); break;
                case ISTouchPhase.Ended:   if (_dragging) EndDrag(); break;
                case ISTouchPhase.Canceled: CancelDrag(); break;
            }
        }

        // EventSystem üzerinden: belirli bir pozisyon UI üzerinde mi?
        // UI üzerinde başlayan dokunuşları oyun girişinden ayırmak için kullanılır.
        // touchId >= 0 → belirli bir parmak, -1 → fare
        private static bool IsOverUI(int touchId = -1)
        {
            if (EventSystem.current == null) return false;
            return touchId >= 0
                ? EventSystem.current.IsPointerOverGameObject(touchId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        // ── Sürükleme mantığı ──────────────────────────────────────────────────

        // Sürükleme başlatır: ekran noktasını masa yüzeyine projeksiyon yap ve başlangıcı kaydet.
        // Not: güç slider'ı sıfırlanmaz — slider bağımsız kontrol edilir, ıstaka sadece yönü değiştirir.
        private void BeginDrag(Vector2 screenPos)
        {
            var world = ScreenToTable(screenPos);
            if (world == null) return;
            _dragging  = true;
        }

        // Her kare sürükleme devam ederken çağrılır.
        // SADECE YÖN (yaw) güncellenir; güç slider tarafından ayrıca kontrol edilir.
        // Parmak X hareketi → ıstaka döner. Y hareketi → hiçbir şey (güç için slider kullanılır).
        private void UpdateDrag(Vector2 screenPos)
        {
            Vector2 delta = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
            delta = Mouse.current.delta.ReadValue(); // fare delta'sı piksel/kare
#else
            if (ISTouch.activeTouches.Count > 0)
                delta = ISTouch.activeTouches[0].delta; // dokunuş delta'sı piksel/kare
#endif

            // X hareketi ile ıstaka döndür
            // RotationSensitivity = 0.15 → her piksel 0.15 derece döndürür
            _yaw += delta.x * RotationSensitivity;

            // Euler açısından quaternion (döndürme matrisi), ondan yön vektörü
            Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
            _targetAimDir = rot * Vector3.forward;

            // Y hareketi kasıtlı olarak yoksayılıyor:
            // Oyuncu ıstakayı geri çekince yön kayıyor, bu deneyimi bozuyor.
            // Güç artık sadece sağdaki slider ile kontrol ediliyor.
        }

        // Nişan yönünü en yakın 1/8 top kalınlığına snap'lar.
        // Hedef top nişan aralığındaysa, parmak tam ortaya veyahut belirli kesrler üzerine kilitlener.
        // rawDir: parmak hareketinden gelen ham nişan yönü
        // Geri dönen değer: snaplı nişan yönü (hedef bulunamazsa ham yön)
        //
        // ŞU AN KULLANILMIYOR: UpdateDrag delta-tabanlı kontrol kullanıyor.
        // Ileride alternatif kontrol moduna geçilirse bu metot devreye girebilir.
        private Vector3 SnapToEighth(Vector3 rawDir)
        {
            if (_cueBall == null) return rawDir;

            const float ballDiameter = 0.18f;
            const float stepSize = ballDiameter / 8f;        // 0.0225 per step
            const float maxPerp = ballDiameter * 7f / 8f;   // 0.1575 — thinnest hit that still collides
            const float showRange = ballDiameter * 5f;       // snap aralığı (görünür mesafe)

            var tc = TableController.Instance;

            // Nişan yönünde en yakın topu bul
            BallController target = NearestBallInAim(
                _cueBall.transform.position, rawDir,
                tc != null ? tc.yellowBall : null,
                tc != null ? tc.redBall : null, showRange);

            if (target == null) return rawDir; // nişan aralığında top yoksa snap yapma

            Vector3 origin = _cueBall.transform.position;
            Vector3 toBall = target.transform.position - origin;
            toBall.y = 0f; // yatay düzlemde çalış
            float dist = toBall.magnitude;
            if (dist < 0.001f) return rawDir;

            // Cross(up, dir) → dir'in SAĞ vektörünü verir (sol değil!)
            // signedPerp > 0 → hedef top nişan doğrusunun sağında
            // signedPerp < 0 → hedef top nişan doğrusunun solunda
            Vector3 rightOfAim = Vector3.Cross(Vector3.up, rawDir).normalized;
            float signedPerp = Vector3.Dot(rightOfAim, toBall);

            // En yakın 1/8 adıma yuvarla ve sınırla
            float snapped = Mathf.Round(signedPerp / stepSize) * stepSize;
            snapped = Mathf.Clamp(snapped, -maxPerp, maxPerp);

            // Nişan yönünü, elde edilen dik mesafeye karşılık gelen açıya döndür.
            // sin(theta) = snapped / dist → theta = arcsin(snapped/dist)
            // Quaternion.AngleAxis(theta, up) → nişan yönünü theta kadar döndür
            float sinTheta = Mathf.Clamp(snapped / dist, -1f, 1f);
            float theta = Mathf.Asin(sinTheta) * Mathf.Rad2Deg;

            return (Quaternion.AngleAxis(theta, Vector3.up) *
                    toBall.normalized).normalized;
        }

        // Nişan yönünde en yakın topu bulur (iki top arasında).
        // Dik mesafesi daha küçük olan (nişana daha yakın olan) tercih edilir.
        private static BallController NearestBallInAim(Vector3 origin, Vector3 dir,
            BallController b1, BallController b2, float showRange)
        {
            float d1 = PerpDistIfInFront(origin, dir, b1);
            float d2 = PerpDistIfInFront(origin, dir, b2);
            bool in1 = d1 >= 0f && d1 < showRange;
            bool in2 = d2 >= 0f && d2 < showRange;
            if (in1 && in2) return d1 <= d2 ? b1 : b2; // ikisi de nişanda → yakın olanı seç
            if (in1) return b1;
            if (in2) return b2;
            return null; // hiçbiri nişan aralığında değil
        }

        // Bir topun nişan doğrusuna dik mesafesini hesaplar.
        // Top nişanın arkasındaysa veya yanda ise -1 döner.
        private static float PerpDistIfInFront(Vector3 origin, Vector3 dir, BallController ball)
        {
            if (ball == null) return -1f;
            Vector3 to = ball.transform.position - origin; to.y = 0f;
            Vector3 d = new Vector3(dir.x, 0f, dir.z).normalized;
            if (Vector3.Dot(d, to.normalized) < 0.05f) return -1f; // top arkada
            return Vector3.Cross(d, to).magnitude; // çapraz çarpım büyüklüğü = dik mesafe
        }

        // Sürükleme biter → sadece sürükleme durumu kapatılır, atış YAPILMAZ.
        // Atış artık sadece power slider bırakıldığında (FireFromSlider) gerçekleşir.
        // Böylece ıstakayı döndürürken yanlışlıkla atış olmaz.
        private void EndDrag() => _dragging = false;

        // Power slider bırakıldığında PowerSliderDragHandler tarafından çağrılır.
        // Slider değerine göre atış gücünü belirler ve animasyonu başlatır.
        public void FireFromSlider()
        {
            if (!_enabled || _cueBall == null) return;
            if (GameController.Instance == null || GameController.Instance.State != GameState.WaitingForShot) return;

            float sliderPower = PowerSlider.Instance != null ? PowerSlider.Instance.Power : 0.5f;
            float power = sliderPower * MaxPower;
            if (power < 0.3f) return; // çok düşük güç, atış yapma

            float pullback = sliderPower * MaxDragDistance * 1.5f + 0.15f;
            StartCoroutine(StrikeAnimation(_cueBall.transform.position, _lastAimDir, power, pullback));
        }

        // Coroutine: ıstaka ileri fırlar (vurma), sonra geri çekilir; tam ortasında top fırlatılır.
        // Animasyon süresi: ileri 0.12s + atış + geri 0.08s
        private IEnumerator StrikeAnimation(Vector3 ballPos, Vector3 dir, float power, float pullback)
        {
            _cueLine.enabled = true;

            float elapsed = 0f;

            // İleri fazı: ıstaka pullback'ten 0'a (topa doğru) ilerler
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                // Lerp: pullback → 0 arasında doğrusal interpolasyon
                float offset = Mathf.Lerp(pullback, 0f, elapsed / 0.12f);
                _cueLine.SetPosition(0, ballPos - dir * 0.12f);
                _cueLine.SetPosition(1, ballPos - dir * (offset + 0.12f));
                yield return null;
            }

            // Animasyonun tam ortasında: top fırlatılır, atış fiziği başlar
            Shoot(dir, power);
            PowerSlider.Instance?.SetPower(0.25f); // slider'ı dinlenme pozisyonuna getir

            // Geri çekilme fazı: ıstaka 0'dan kısmen geri çekilir
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Lerp(0f, pullback * 0.4f, elapsed / 0.08f);
                _cueLine.SetPosition(1, ballPos - dir * (offset + 0.12f));
                yield return null;
            }
        }

        // ── Fizik yardımcıları ────────────────────────────────────────────────

        // Gerçek atışı gerçekleştirir: CueController'ı devre dışı bırakır, topa kuvvet uygular.
        private void Shoot(Vector3 direction, float power)
        {
            Disable(); // ıstakayı gizle (top hareket ederken görünmemeli)

            // HitPointSelector'den spin değerini al (oyuncu hangi noktaya vurduğunu seçti)
            var spin = HitPointSelector.Instance != null
                ? HitPointSelector.Instance.SelectedOffset
                : Vector2.zero;

            // BallController'a kuvvet ve spin bilgisini gönder; fizik motoru hesaplar
            _cueBall.ApplyForce(direction * power, spin);

            // GameController ve TableController'ı haberdar et
            GameController.Instance.OnShotFired();
            TableController.Instance.OnShotFired();
        }

        // Nişan çizgisinin 3D noktalarını hesaplar.
        // Kural: ilk top temasına veya ilk banttan sonraya kadar çizilir, daha fazla değil.
        // Neden? İlk banttan sonraki gidişat spin'e göre değişir, doğru tahmin gösterilemez.
        private List<Vector3> CalculateTrajectory(Vector3 origin, Vector3 dir)
        {
            var pts = new List<Vector3> { origin };
            var d = new Vector3(dir.x, 0f, dir.z).normalized;

            // Nişan doğrusunun X ve Z bantlarına olan parametrik mesafesi (t = mesafe).
            // Nişan doğrusu: P(t) = origin + d * t
            // X bantına çarpma: origin.x + d.x * t = ±_halfX → t = (±_halfX - origin.x) / d.x
            float tX = d.x > 0f ? (_halfX - origin.x) / d.x
                     : d.x < 0f ? (-_halfX - origin.x) / d.x
                     : float.MaxValue; // d.x = 0 ise X bantına çarpmaz
            float tZ = d.z > 0f ? (_halfZ - origin.z) / d.z
                     : d.z < 0f ? (-_halfZ - origin.z) / d.z
                     : float.MaxValue;
            float tCushion = Mathf.Min(tX, tZ); // hangisi daha yakınsa o bant

            // Bu nişanda bir top var mı? varsa ne kadar uzakta?
            float tBall = FindFirstBallHit(origin, d);

            if (tBall > 0.05f && tBall < tCushion)
                pts.Add(origin + d * tBall);    // top banda göre daha yakında: top temas noktasında dur
            else
                pts.Add(origin + d * tCushion); // bant daha yakında: bant yüzeyinde dur

            return pts;
        }

        // Nişan doğrusunun (ışının) bir hedef topa çarpıp çarpmadığını kontrol eder.
        // Fiziksel hesap: ışın-küre kesişimi (ray-sphere intersection) 2D versiyonu (XZ düzlemi).
        //
        // Matematiksel arka plan:
        //   Nişan ışını: P(t) = origin + dir * t
        //   Küre: |P - center|² = contactR²
        //   İkisini birleştirip açarsak: at² + bt + c = 0 (a=1 çünkü dir normalize)
        //   Çözüm: t = (-b ± sqrt(b²-4c)) / 2
        //   disc < 0 → kesişim yok (ışın kürenin yanından geçiyor)
        //   disc >= 0 → küçük t değeri = ön yüzey teması noktası
        private static float FindFirstBallHit(Vector3 origin, Vector3 dir)
        {
            var tc = TableController.Instance;
            if (tc == null) return float.MaxValue;

            const float contactR = 0.18f; // iki topun yarıçapları toplamı (0.09 + 0.09)
            float closest = float.MaxValue;

            foreach (var ball in new[] { tc.yellowBall, tc.redBall })
            {
                if (ball == null) continue;

                // 2D yatay düzlemde (Y bileşeni sıfır)
                Vector3 center = new(ball.transform.position.x, 0f, ball.transform.position.z);
                Vector3 orig2d = new(origin.x, 0f, origin.z);
                Vector3 oc = orig2d - center; // origin'den top merkezine vektör

                float b = 2f * Vector3.Dot(oc, dir);
                float c = Vector3.Dot(oc, oc) - contactR * contactR;
                float disc = b * b - 4f * c; // diskriminant: negatifse kesişim yok

                if (disc < 0f) continue; // ışın topu kaçırdı

                float t = (-b - Mathf.Sqrt(disc)) * 0.5f; // ön yüzey teması (küçük t)
                if (t > 0.05f && t < closest) // arkada değil ve şimdiye kadar en yakın
                    closest = t;
            }

            return closest;
        }

        // Ekran koordinatını masa yüzeyindeki dünya koordinatına çevirir.
        // Plane(up, zero) → Y=0 düzlemi (masa yüzeyi)
        // Camera.ScreenPointToRay → kameradan tıklanan ekran noktasına ışın
        // Plane.Raycast → ışının düzlemi nerede kestiğini hesaplar
        private Vector3? ScreenToTable(Vector2 screenPos)
        {
            var ray = _cam.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero); // Y=0 masa düzlemi
            if (plane.Raycast(ray, out float enter))
                return ray.GetPoint(enter); // enter = ışının düzleme olan mesafesi
            return null; // ışın düzleme çarpmadı (olmamalı ama güvenlik için null döndür)
        }
    }
}
