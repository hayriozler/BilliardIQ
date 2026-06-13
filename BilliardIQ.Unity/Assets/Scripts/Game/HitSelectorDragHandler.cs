using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Game
{
    // HitSelectorDragHandler → sol alttaki vurma noktası seçici dairesine eklenir.
    // Parmak basıldığında daireyi büyütür (daha kolay seçim), sürükleme ile kırmızı noktayı hareket ettirir.
    // Parmak kaldırıldığında daireyi küçültür.
    //
    // Unity UI event arayüzleri: Bu arayüzleri implemente eden bileşen,
    // bir Canvas üzerindeki GraphicRaycaster tarafından otomatik bulunur ve çağrılır.
    // EventSystem → tüm UI olaylarının merkezi; hangi objenin üzerine dokunuldu takip eder.
    public class HitSelectorDragHandler : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private HitPointSelector _selector;  // vurma noktasını gerçekte tutan script
        private RectTransform    _circleRt;  // büyük dairenin RectTransform'u (zoom için)
        private RectTransform    _dotRt;     // kırmızı noktanın RectTransform'u
        private float            _radius;    // dairenin piksel yarıçapı

        // Zoom animasyonu parametreleri
        private const float _zoomedScale   = 2.8f;  // basıldığında kaç kat büyüsün
        private const float _animDuration  = 0.14f; // zoom animasyonu süresi (saniye)

        // Parmağın tam altında olan nokta parmak altında kalır; yukarı kaydırarak görünür kılıyoruz.
        private const float _fingerOffset  = 40f;   // noktayı parmaktan kaç piksel yukarı kaldır

        // Devam eden zoom animasyonu. Yeni animasyon başlamadan önce eskisi durdurulur.
        private Coroutine _scaleAnim;

        // UIManager tarafından çağrılır; bu handler'ın ihtiyaç duyduğu referansları atar.
        public void Init(HitPointSelector selector,
                         RectTransform circleRt,
                         RectTransform dotRt,
                         float radius)
        {
            _selector = selector;
            _circleRt = circleRt;
            _dotRt    = dotRt;
            _radius   = radius;
        }

        // Parmak daireye bastığında çağrılır: daireyi büyüt ve vurma noktasını hemen güncelle.
        public void OnPointerDown(PointerEventData data)
        {
            AnimateTo(_zoomedScale);
            Process(data);
        }

        // Parmak sürüklenirken her kare çağrılır: noktanın pozisyonunu güncelle.
        public void OnDrag(PointerEventData data) => Process(data);

        // Parmak kaldırıldığında: daireyi normal boyutuna döndür.
        public void OnPointerUp(PointerEventData data) => AnimateTo(1f);

        // Dokunma pozisyonunu hesaplayıp HitPointSelector'e bildirir.
        private void Process(PointerEventData data)
        {
            // Ekran koordinatını dairenin yerel koordinat sistemine çevir.
            // ScreenSpaceOverlay canvas → kamera parametresi null geçilir.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _circleRt, data.position, null, out Vector2 local)) return;

            // RectTransform pivot'u sol-alt (0,0); merkeze göre koordinata çevir.
            // _radius = daire yarıçapı, merkez = (radius, radius)
            local -= new Vector2(_radius, _radius);

            // Daire içinde kal: dışarıya taşarsa normalize et ve yarıçapa kırp.
            if (local.magnitude > _radius)
                local = local.normalized * _radius;

            // Kırmızı noktayı parmaktan yukarı kaydır (sadece görsel — spin hesabını etkilemez).
            Vector2 display = local + new Vector2(0f, _fingerOffset);
            if (display.magnitude > _radius)
                display = display.normalized * _radius;

            _dotRt.anchoredPosition = display;         // görsel noktayı taşı
            _selector.SetHitPoint(local, _radius);     // spin için orijinal pozisyonu kullan
        }

        // Hedef ölçeğe yumuşak animasyonla geçer.
        // Önceki animasyon sürüyorsa önce durdurulur.
        private void AnimateTo(float target)
        {
            if (_scaleAnim != null) StopCoroutine(_scaleAnim);
            _scaleAnim = StartCoroutine(ScaleRoutine(target));
        }

        // Coroutine: _animDuration süresinde mevcut ölçekten hedef ölçeğe geçer.
        // SmoothStep → ease in-out yumuşatma (başında ve sonunda yavaşlar).
        private IEnumerator ScaleRoutine(float target)
        {
            float start   = _circleRt.localScale.x; // mevcut ölçek (x,y,z eşit olduğu için sadece x alınır)
            float elapsed = 0f;

            while (elapsed < _animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animDuration);
                // Vector3.one * değer → (değer, değer, değer) vektörü oluşturur (uniform scale)
                _circleRt.localScale = Vector3.one * Mathf.Lerp(start, target, t);
                yield return null; // bir kare bekle, tekrar devam et
            }

            // Animasyon bitince kesin değere atla (float birikme hatalarını önle)
            _circleRt.localScale = Vector3.one * target;
            _scaleAnim = null;
        }
    }
}
