using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Game
{
    // PowerSlider → ekranın sağ tarafındaki dikey güç göstergesi.
    // Oyuncu aşağı sürükledikçe atış gücü artar (0 = minimum, 1 = maksimum).
    // Dolgu çubuğu yukarıdan aşağı büyür; ıstaka göstergesi aşağı kayar.
    // CueController bu değeri okuyarak atış kuvvetini hesaplar.
    public class PowerSlider : MonoBehaviour
    {
        // Singleton: PowerSlider.Instance.Power ile her yerden okunabilir.
        public static PowerSlider Instance { get; private set; }

        // Anlık güç seviyesi: 0 = en az, 1 = tam güç. CueController tarafından okunur.
        public float Power { get; private set; } = 0.25f;

        // Yeşil dolgu dikdörtgeni: yüksekliği Power * trackH kadar büyür.
        private RectTransform _fillRt;

        // Kahverengi ıstaka göstergesi: Power arttıkça aşağı kayar.
        private RectTransform _cueRt;

        // Slider parçasının toplam piksel yüksekliği (UIManager'dan gelir).
        private float _trackH;

        void Awake() { Instance = this; }

        // UIManager slider görsellerini oluşturduktan sonra bu metodu çağırır.
        // trackRect  = sürükleme algılayan arka plan dikdörtgeni
        // fillRt     = dolgu çubuğunun RectTransform'u
        // cueRt      = ıstaka çubuğunun RectTransform'u
        // trackHeight = slider yüksekliği (piksel)
        public void InitUI(RectTransform trackRect,
                           RectTransform fillRt,
                           RectTransform cueRt,
                           float trackHeight)
        {
            _fillRt  = fillRt;
            _cueRt   = cueRt;
            _trackH  = trackHeight;

            RefreshVisuals(); // başlangıç görselini ayarla

            // PowerSliderDragHandler: dokunmatik/fare sürükleme olaylarını dinler ve SetPower'ı çağırır.
            var handler = trackRect.gameObject.AddComponent<PowerSliderDragHandler>();
            handler.Init(this, trackRect, trackHeight);
        }

        // Güç seviyesini ayarlar. PowerSliderDragHandler her sürüklemede çağırır.
        // CueController da atış sonrası sıfırlamak için çağırır.
        // t = [0, 1] aralığında herhangi bir değer (Clamp01 dışına taşmayı engeller)
        public void SetPower(float t)
        {
            Power = Mathf.Clamp01(t);
            RefreshVisuals();
        }

        // Görsel elemanları güç değerine göre günceller.
        private void RefreshVisuals()
        {
            if (_fillRt == null) return;

            // Dolgu yüksekliği = Power * toplam yükseklik
            // anchorMin/Max Y = 1 (üstte sabit) → pivot üstte → aşağı doğru büyür
            float fillH = Power * _trackH;
            _fillRt.sizeDelta        = new Vector2(_fillRt.sizeDelta.x, fillH);
            _fillRt.anchoredPosition = Vector2.zero; // pivot üstte olduğu için Y değişmez

            // Istaka göstergesi: üstte = minimum güç, altta = maksimum güç
            // Orta Y ekseni (anchorMin Y = 0.5) referans alınır, Power arttıkça aşağı kayar.
            if (_cueRt != null)
                _cueRt.anchoredPosition = new Vector2(0f, _trackH * 0.5f - Power * _trackH);
        }
    }

    // PowerSliderDragHandler → slider'a dokunma/sürükleme olaylarını yakalar.
    // Unity UI event sistemiyle entegre: IPointerDownHandler, IDragHandler, IPointerUpHandler arayüzleri.
    // Bu arayüzleri implement eden bir bileşen, Unity'nin EventSystem'i tarafından otomatik çağrılır.
    public class PowerSliderDragHandler : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private PowerSlider   _slider;
        private RectTransform _trackRt;
        private float         _trackH;

        // PowerSlider.InitUI() tarafından çağrılır; referansları atar.
        public void Init(PowerSlider slider, RectTransform trackRt, float trackHeight)
        {
            _slider  = slider;
            _trackRt = trackRt;
            _trackH  = trackHeight;
        }

        // Parmak/fare slider'a basıldığında çağrılır.
        // PointerEventData → dokunulan pozisyon, hangi parmak, vs. bilgilerini içerir.
        public void OnPointerDown(PointerEventData data) { Debug.Log($"[PS] PointerDown pos={data.position}"); Process(data); }

        // Parmak slider üzerinde hareket ederken sürekli çağrılır.
        public void OnDrag(PointerEventData data)        { Debug.Log($"[PS] Drag pos={data.position}"); Process(data); }

        // Parmak/fare kaldırıldığında çağrılır → atışı tetikle.
        public void OnPointerUp(PointerEventData data)
        {
            // Minimum güçten fazlaysa atışı başlat.
            // Çok düşük güçte yanlışlıkla slider'a dokunulmuş olabilir.
            if (_slider.Power > 0.05f)
                CueController.Instance?.FireFromSlider();
        }

        // Dokunma pozisyonunu güç değerine çevirir.
        private void Process(PointerEventData data)
        {
            // ScreenPointToLocalPointInRectangle → ekran koordinatını RectTransform yerel koordinatına çevirir.
            // Canvas ScreenSpaceOverlay modunda kamera null geçilir.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _trackRt, data.position, null, out Vector2 local)) return;

            // local.y aralığı: üst = +trackH/2, alt = -trackH/2
            // Üst = minimum güç, alt = maksimum güç (aşağı sürükleme = güç artışı)
            // Formül: t = 0 (üst) → t = 1 (alt)
            float t = Mathf.Clamp01((-local.y + _trackH * 0.5f) / _trackH);
            _slider.SetPower(t);
        }
    }
}
