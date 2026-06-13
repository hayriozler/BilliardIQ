using UnityEngine;

namespace Assets.Scripts.Game
{
    // HitPointSelector → oyuncunun ıstakanın topa nerede vurduğunu seçmesini sağlar.
    // Ekranın sol alt köşesinde küçük bir daire var; ortasındaki kırmızı noktayı sürükleyerek
    // vurma noktasını seçersin.
    //
    // SelectedOffset değerleri:
    //   (0, 0)    → tam ortadan vurma → spin yok
    //   (+1, 0)   → sağ kenardan    → sağ İngiliz (sağa spin)
    //   (-1, 0)   → sol kenardan    → sol İngiliz
    //   (0, +1)   → üst kenardan    → pika (follow/topspin)
    //   (0, -1)   → alt kenardan    → çekme (draw/backspin)
    public class HitPointSelector : MonoBehaviour
    {
        // Singleton: HitPointSelector.Instance ile erişilir.
        public static HitPointSelector Instance { get; private set; }

        // Seçilen vurma noktası: [-1, +1] aralığında normalize edilmiş (X = yan, Y = yukarı/aşağı).
        // CueController.Shoot() bu değeri BallController.ApplyForce()'a spin olarak gönderir.
        public Vector2 SelectedOffset { get; private set; } = Vector2.zero;

        // Seçici daima açık; hiçbir zaman CueController girişini engellemez.
        // (Eski tasarımda bir popup açılıyordu, artık sabit bir daire var.)
        public bool IsOpen => false;

        // Kırmızı noktanın UI RectTransform'u (görsel pozisyon için)
        private RectTransform _dotRt;

        void Awake() => Instance = this;

        // TableController.Start() tarafından çağrılır. Gelecekte spin fiziğiyle ilişkilendirmek için ayrıldı.
        public void Init(BallController whiteBall) { }

        // UIManager, canvas hazır olduktan sonra bu metodu çağırır.
        // ballCircleRect = büyük dairenin RectTransform'u (sürükleme alanı)
        // dotRt          = küçük kırmızı noktanın RectTransform'u
        // radius         = dairenin yarıçapı (piksel cinsinden)
        public void InitUI(RectTransform ballCircleRect, RectTransform dotRt, float radius)
        {
            _dotRt = dotRt;
            // HitSelectorDragHandler bileşenini dairenin objesine ekle; o sürükleme olaylarını dinler.
            var handler = ballCircleRect.gameObject.AddComponent<HitSelectorDragHandler>();
            handler.Init(this, ballCircleRect, dotRt, radius);
        }

        // HitSelectorDragHandler sürükleme sırasında bu metodu çağırır.
        // localPos = daire merkezine göre dokunulan pozisyon (zaten kırpılmış ve parmak üstüne kaydırılmış)
        // radius   = dairenin yarıçapı
        // Sonuç: normalize [-1, 1] aralığında SelectedOffset → CueController'a spin olarak gönderilir.
        public void SetHitPoint(Vector2 localPos, float radius)
        {
            SelectedOffset = localPos / radius; // yarıçapa bölerek normalize et
        }
    }
}
