using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Assets.Scripts.Game
{
    // UIManager → tüm ekran üstü arayüz elemanlarını (HUD) oluşturur ve düzenler.
    //
    // Unity UI sistemi (uGUI):
    //   Canvas → tüm UI elemanlarının konulduğu sanal ekran. ScreenSpaceOverlay modunda
    //            dünya 3D sahnesinin üzerine çizilir, kameradan bağımsızdır.
    //   RectTransform → UI elemanlarının konumu ve boyutu için Transform yerine kullanılır.
    //                   anchorMin/Max ile ekranın hangi köşesine/ortasına sabitlendiği belirlenir.
    //   Image → renkli dikdörtgen veya sprite görüntüler.
    //   Button → tıklanabilir eleman; onClick event'ine listener eklenir.
    //   CanvasScaler → farklı ekran çözünürlüklerinde UI elemanlarını ölçekler.
    //   GraphicRaycaster → mouse/dokunuş olaylarının hangi UI elemanına gittiğini hesaplar.
    //   EventSystem → UI olay yöneticisi; tek olmalı, yoksa oluşturulur.
    public class UIManager : MonoBehaviour
    {
        private CameraController _cam; // kamera toggle butonu için referans

        // Start() → sahne yüklendikten sonra çalışır. CameraController'ın Start'ı da bu noktada hazır.
        void Start()
        {
            _cam = FindAnyObjectByType<CameraController>();
            BuildCanvas(); // tüm UI'ı oluştur
        }

        // Ana canvas'ı ve tüm HUD elemanlarını oluşturur.
        private void BuildCanvas()
        {
            // EventSystem yoksa oluştur: UI olayları (tıklama, sürükleme) için şart.
            // InputSystemUIInputModule → Unity New Input System ile uyumlu (eskisi StandaloneInputModule).
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            // Canvas oluştur: ScreenSpaceOverlay → her zaman en üstte, kameradan bağımsız
            var go     = new GameObject("HUD_Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // diğer canvas'ların üstünde çizilmesini sağlar

            // CanvasScaler: 1920x1080 referans çözünürlüğüne göre ölçekle.
            // matchWidthOrHeight = 0.5 → genişlik ve yükseklik eşit ağırlıkla hesaba katılır.
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            // GraphicRaycaster → dokunuş/fare pozisyonlarını UI elemanlarına yönlendirir.
            go.AddComponent<GraphicRaycaster>();

            AddBackButton(go);
            AddCameraToggleButton(go);
            AddHitPointSelector(go);
            AddPowerSlider(go);
            AddThicknessIndicator(go);
        }

        // ── Geri butonu (sol üst) ────────────────────────────────────────────

        // Oyuncuyu Unity'den MAUI'ye döndüren yuvarlak buton.
        private void AddBackButton(GameObject canvas)
        {
            const int SIZE = 110;
            var go = MakeCircleButton(canvas, "Btn_Back",
                anchor: new Vector2(0f, 1f),      // sol üst köşeye sabitle
                pos: new Vector2(28f, -28f),       // kenardan 28 piksel içeride
                size: SIZE,
                onClick: OnBackClicked);

            // "<" işareti üstte büyük, "BACK" yazısı altta küçük
            AddSplitLabel(go, "<", "BACK", 50, 22, Color.white,
                new Color(0.65f, 1f, 0.65f, 0.95f));
        }

        // Geri butonu tıklanınca: MauiBridge aracılığıyla Unity Activity'yi kapat, MAUI'ye dön.
        private void OnBackClicked() => MauiBridge.ExitToMaui();

        // ── Kamera toggle butonu (sağ üst) ───────────────────────────────────

        // Kamera açısını döngüsel değiştirir: 3D → Üstten → Yan → 3D
        private void AddCameraToggleButton(GameObject canvas)
        {
            const int SIZE = 100;
            var go = MakeCircleButton(canvas, "Btn_CamToggle",
                anchor: new Vector2(1f, 1f),       // sağ üst köşeye sabitle
                pos: new Vector2(-28f, -28f),       // kenardan 28 piksel içeride
                size: SIZE,
                onClick: OnCamToggleClicked);

            AddSplitLabel(go, "3D", "VIEW", 36, 20, Color.white,
                new Color(0.65f, 1f, 0.65f, 0.95f));
            _camIconLabel = go.transform.Find("Icon").GetComponent<Text>();
        }

        private Text _camIconLabel;
        private int  _camIndex = 0; // 0=3D, 1=Üstten, 2=Yan
        private static readonly string[] _camIcons = { "3D", "TOP", "SIDE" };

        // Her tıklamada sıradaki kamera açısına geç.
        private void OnCamToggleClicked()
        {
            if (_cam == null) return;
            // % 3 → 0,1,2 döngüsü: 2'den sonra tekrar 0'a döner
            _camIndex = (_camIndex + 1) % 3;
            if (_camIconLabel != null) _camIconLabel.text = _camIcons[_camIndex];
            switch (_camIndex)
            {
                case 0: _cam.GoToAngled(); break; // 3D genel görünüm
                case 1: _cam.GoToTop();    break; // kuşbakışı
                case 2: _cam.GoToSide();   break; // yan görünüm
            }
        }

        // ── Vurma noktası seçici (sol alt) ───────────────────────────────────
        // Oyuncunun ıstakanın topa nerede vurduğunu seçtiği büyük beyaz daire.
        // İçindeki kırmızı nokta spin bilgisini belirler.

        private void AddHitPointSelector(GameObject canvas)
        {
            const int CIRCLE = 170; // daire piksel boyutu
            const int DOT    = 28;  // kırmızı noktanın piksel boyutu
            float     radius = CIRCLE / 2f; // 85 piksel yarıçap

            var go = new GameObject("HitSelector");
            go.transform.SetParent(canvas.transform, false);

            // RectTransform: sol alt köşeye sabitle (anchorMin=anchorMax=pivot=0,0)
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(28f, 28f); // köşeden 28px içeride
            rt.sizeDelta = new Vector2(CIRCLE, CIRCLE);

            // Arka plan: beyaz dolu daire (cue ball yüzü)
            var bg    = go.AddComponent<Image>();
            bg.sprite = MakeCircleSprite(CIRCLE,
                fill:     new Color(0.95f, 0.95f, 0.95f, 1f), // açık gri/beyaz
                border:   new Color(0.40f, 0.40f, 0.40f, 1f), // koyu gri çerçeve
                borderPx: 5);
            bg.raycastTarget = true; // dokunuş algıla (sürükleme buradan başlar)

            // Orta çember rehber halkası: yarım yarıçap büyüklüğünde yarı saydam halka
            AddRing(go, Mathf.RoundToInt(CIRCLE * 0.5f),
                new Color(0.70f, 0.70f, 0.70f, 0.35f), borderPx: 2);

            // Yatay ve dikey ince çizgiler (nişan merkezi için görsel yardımcı)
            AddLine(go, new Vector2(CIRCLE - 12, 2), new Color(0.55f, 0.55f, 0.55f, 0.50f));
            AddLine(go, new Vector2(2, CIRCLE - 12), new Color(0.55f, 0.55f, 0.55f, 0.50f));

            // Kırmızı nokta: oyuncunun seçtiği vurma noktası
            var dotGo = new GameObject("HitDot");
            dotGo.transform.SetParent(go.transform, false);
            var dotRt = dotGo.AddComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(DOT, DOT);
            dotRt.anchoredPosition = Vector2.zero; // başlangıçta daire merkezinde
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.sprite = MakeCircleSprite(DOT,
                fill:     new Color(0.88f, 0.10f, 0.10f, 1f), // kırmızı
                border:   new Color(0.50f, 0.00f, 0.00f, 1f), // koyu kırmızı çerçeve
                borderPx: 2);
            dotImg.raycastTarget = false; // dokunuş algılamaz (üstündeki daire algılar)

            // HitPointSelector script'ini bilgilendir: hangi RectTransform'lar var?
            HitPointSelector.Instance?.InitUI(rt, dotRt, radius);
        }

        // ── Güç göstergesi (sağ kenar) ───────────────────────────────────────
        // Oyuncu aşağı sürükledikçe güç artar; bırakınca atış yapılır.

        private void AddPowerSlider(GameObject canvas)
        {
            const int _w       = 96;  // slider genişliği (piksel)
            const int _h       = 420; // slider yüksekliği (piksel)
            const int _cueH   = 120; // ıstaka gösterge çubuğu yüksekliği

            var go = new GameObject("PowerSlider");
            go.transform.SetParent(canvas.transform, false);

            // Sağ-orta köşeye sabitle (anchorMin/Max/pivot = 1, 0.5)
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-28f, 0f); // sağ kenardan 28px içeride
            rt.sizeDelta = new Vector2(_w, _h);

            // Koyu arka plan: slider'ın zemini
            var bg    = go.AddComponent<Image>();
            bg.color  = new Color(0.08f, 0.08f, 0.08f, 0.80f);
            bg.raycastTarget = true; // sürükleme bu obje üzerinde başlar

            // Yeşil dolgu: güç seviyesini gösterir, yukarıdan aşağı büyür
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.1f, 1f); // üst kenarına sabitle
            fillRt.anchorMax = new Vector2(0.9f, 1f);
            fillRt.pivot     = new Vector2(0.5f, 1f); // pivot üstte: aşağı doğru büyüyecek
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(0f, 0f);    // yükseklik PowerSlider.RefreshVisuals'ta ayarlanır
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.20f, 0.85f, 0.35f, 0.95f); // yeşil
            fillImg.raycastTarget = false;

            // Kahverengi ıstaka çubuğu: güç arttıkça aşağı kayar (görsel ipucu)
            var cueGo = new GameObject("CueIndicator");
            cueGo.transform.SetParent(go.transform, false);
            var cueRt = cueGo.AddComponent<RectTransform>();
            cueRt.anchorMin = new Vector2(0f, 0.5f);
            cueRt.anchorMax = new Vector2(1f, 0.5f);
            cueRt.pivot     = new Vector2(0.5f, 0.5f);
            cueRt.sizeDelta = new Vector2(0f, _cueH);
            cueRt.anchoredPosition = new Vector2(0f, _h * 0.5f); // başta en üstte (min güç)
            var cueImg = cueGo.AddComponent<Image>();
            cueImg.color = new Color(0.65f, 0.42f, 0.18f, 1f); // tahta kahverengisi
            cueImg.raycastTarget = false;

            // "POWER" yazısı: 90° döndürülmüş, slider ortasında dikey
            AddRotatedLabel(go, "POWER", _h);

            // MIN / MAX etiketleri: slider'ın üstüne ve altına
            AddSmallLabel(go, "MAX",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 14f));
            AddSmallLabel(go, "MIN",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f));

            // PowerSlider bileşeni yoksa ekle, sonra InitUI çağır
            if (PowerSlider.Instance == null)
                go.AddComponent<PowerSlider>();
            PowerSlider.Instance.InitUI(rt, fillRt, cueRt, _h);
        }

        // ── Kalınlık göstergesi (alt orta, iki daire) ─────────────────────────
        // Hedef topa olan çarpışma kalınlığını iki üst üste binen daire ile gösterir.
        //   Tam üst üste = tam ortadan vurma (4/4)
        //   Kenardan değme = ince vurma (1/4)
        //   Ayrılmış = ıska

        private void AddThicknessIndicator(GameObject canvas)
        {
            const float D   = 110f;   // daire piksel çapı
            const float containerWidth  = 310f;
            const float containerHeight  = 120f;
            const float tgX = 50f;    // hedef top dairesinin container merkezi içindeki X pozisyonu
            const float ballY = 14f;  // dairelerin container içindeki dikey ofseti

            // Bu iki değer ThicknessIndicator'e verilir; cue ball pozisyonunu hesaplarken kullanır
            ThicknessIndicator.BallDisplayDiameter = D;
            ThicknessIndicator.TargetCenterX       = tgX;

            // ── Container ─────────────────────────────────────────────────────
            var go = new GameObject("ThicknessIndicator");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // alt orta
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 20f); // alt kenardan 20px yukarıda
            rt.sizeDelta = new Vector2(containerWidth, containerHeight);

            var panel = go.AddComponent<Image>();
            panel.color = new Color(0.05f, 0.05f, 0.05f, 0.55f); // yarı saydam koyu panel
            panel.raycastTarget = false;

            // CanvasGroup → tüm paneli alpha ile gizle/göster.
            // ThicknessIndicator: topa nişan alınca alpha=1, yoksa alpha=0
            var group = go.AddComponent<CanvasGroup>();
            group.alpha          = 0f;   // başta gizli
            group.blocksRaycasts = false; // gizliyken bile dokunuş olaylarını bloklamasın
            group.interactable   = false;

            // ── Daireler alanı ─────────────────────────────────────────────────
            var ballsGo = new GameObject("BallsArea");
            ballsGo.transform.SetParent(go.transform, false);
            var ballsRt = ballsGo.AddComponent<RectTransform>();
            ballsRt.anchorMin = ballsRt.anchorMax = ballsRt.pivot = new Vector2(0.5f, 0.5f);
            ballsRt.anchoredPosition = new Vector2(0f, ballY);
            ballsRt.sizeDelta = new Vector2(containerWidth, D);

            // Hedef top dairesi (önce çizilir → arkada kalır)
            var tgGo = new GameObject("TargetBall_Ind");
            tgGo.transform.SetParent(ballsGo.transform, false);
            var tgRt = tgGo.AddComponent<RectTransform>();
            tgRt.anchorMin = tgRt.anchorMax = tgRt.pivot = new Vector2(0.5f, 0.5f);
            tgRt.anchoredPosition = new Vector2(tgX, 0f); // container merkezi içinde tgX kadar sağda
            tgRt.sizeDelta = new Vector2(D, D);
            var tgImg = tgGo.AddComponent<Image>();
            tgImg.sprite = MakeCircleSprite((int)D,
                fill:     new Color(1f, 0.82f, 0f, 1f), // sarı (ThicknessIndicator rengi değiştirebilir)
                border:   new Color(0.5f, 0.4f, 0f, 1f),
                borderPx: 6);
            tgImg.raycastTarget = false;

            // Beyaz top dairesi (üstte çizilir, hareket eder)
            var cbGo = new GameObject("CueBall_Ind");
            cbGo.transform.SetParent(ballsGo.transform, false);
            var cbRt = cbGo.AddComponent<RectTransform>();
            cbRt.anchorMin = cbRt.anchorMax = cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.anchoredPosition = new Vector2(tgX - D, 0f); // başta: sadece kenarlar değiyor (ıska)
            cbRt.sizeDelta = new Vector2(D, D);
            var cbImg = cbGo.AddComponent<Image>();
            cbImg.sprite = MakeCircleSprite((int)D,
                fill:     new Color(0.96f, 0.96f, 0.96f, 0.97f), // beyaz
                border:   new Color(0.30f, 0.30f, 0.30f, 1f),
                borderPx: 6);
            cbImg.raycastTarget = false;

            // ThicknessIndicator script'ini bağla
            var ind = go.AddComponent<ThicknessIndicator>();
            ind.InitUI(cbRt, tgImg, group);
        }

        // ── Yardımcı metodlar ─────────────────────────────────────────────────

        // Yuvarlak görünümlü buton oluşturur: Image + Button bileşenleri.
        // anchor = ekranın hangi köşesine/kenarına sabitlensin (0=sol/alt, 1=sağ/üst)
        // onClick = tıklanınca çağrılacak metot
        private GameObject MakeCircleButton(GameObject canvas, string name,
            Vector2 anchor, Vector2 pos, int size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(size,
                fill:     new Color(0.04f, 0.16f, 0.08f, 0.93f), // koyu yeşil
                border:   new Color(0.25f, 0.80f, 0.45f, 1f),    // parlak yeşil çerçeve
                borderPx: 5);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // basıldığında gri
            btn.colors = colors;
            btn.onClick.AddListener(onClick); // event listener: tıklanınca onClick çağrılır
            return go;
        }

        // Dolu veya çerçeveli daire şeklinde Sprite oluşturur.
        // Texture2D → piksel piksel çizilen doku; daire kenarı antialiasing olmadan keskin.
        // Sprite.Create → bu dokuyu UI için kullanılabilir Sprite'a çevirir.
        private static Sprite MakeCircleSprite(int size, Color fill, Color border, int borderPx)
        {
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size / 2f, cy = size / 2f;
            float outerR = cx - 1f;           // dıştaki en son piksel yarıçapı
            float innerR = cx - borderPx;     // çerçeve içi yarıçap

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Pitagor teoremi: bu pikselin daire merkezine uzaklığı
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                Color c = d > outerR ? Color.clear : // daire dışı: şeffaf
                          d > innerR ? border :       // çerçeve bandı
                                       fill;          // daire içi
                tex.SetPixel(x, y, c);
            }
            tex.Apply(); // değişiklikleri GPU'ya yükle
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // Sadece çerçevesi olan (içi şeffaf) halka ekler. Hit selector'deki rehber çember için kullanılır.
        private static void AddRing(GameObject parent, int diameter, Color color, int borderPx)
        {
            var go = new GameObject("Ring");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = Vector2.zero; // parent merkezinde
            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(diameter,
                fill:     Color.clear, // içi boş
                border:   color,
                borderPx: borderPx);
            img.raycastTarget = false;
        }

        // Yatay veya dikey ince çizgi ekler (hit selector çapraz çizgileri için).
        private static void AddLine(GameObject parent, Vector2 size, Color color)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        // İki satırlı buton etiketi: üstte büyük ikon yazısı, altta küçük açıklama.
        private static void AddSplitLabel(GameObject parent,
            string topText, string botText,
            int topSize, int botSize,
            Color topColor, Color botColor)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Üst yazı (örn. "<" veya "3D")
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(parent.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.30f); // alt %30'u alt etikete bırak
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
            var iconTxt = iconGo.AddComponent<Text>();
            iconTxt.text = topText; iconTxt.font = font;
            iconTxt.fontSize = topSize; iconTxt.color = topColor;
            iconTxt.alignment = TextAnchor.MiddleCenter;

            // Alt yazı (örn. "BACK" veya "VIEW")
            var lblGo = new GameObject("SubLabel");
            lblGo.transform.SetParent(parent.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = new Vector2(1f, 0.35f); // alttan %35
            lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;
            var lblTxt = lblGo.AddComponent<Text>();
            lblTxt.text = botText; lblTxt.font = font;
            lblTxt.fontSize = botSize; lblTxt.color = botColor;
            lblTxt.alignment = TextAnchor.MiddleCenter;
        }

        // 90° döndürülmüş "POWER" yazısını slider üzerinde dikey olarak ekler.
        // Quaternion.Euler(0,0,90) → Z ekseni etrafında 90° döndürme = yatay yazı dikey hale gelir.
        private static void AddRotatedLabel(GameObject parent, string text, float sliderH)
        {
            var go = new GameObject("PowerLabel");
            go.transform.SetParent(parent.transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // dikey yaz
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(sliderH, 36f); // döndürülmüş olduğu için genişlik = slider yüksekliği
            rt.anchoredPosition = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(0.80f, 0.80f, 0.80f, 0.75f); // yarı saydam gri
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
        }

        // Küçük etiket ekler (MIN, MAX).
        // anchorMin/Max/pivot → slider'ın üst veya alt kenarına sabitle.
        private static void AddSmallLabel(GameObject parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos)
        {
            var go = new GameObject("Lbl_" + text);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(0f, 28f);
            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = new Color(0.80f, 0.80f, 0.80f, 0.85f);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
        }
    }
}
