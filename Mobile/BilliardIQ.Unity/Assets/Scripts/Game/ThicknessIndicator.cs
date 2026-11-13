using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Game
{
    // ThicknessIndicator → ekranın altında iki daire gösterir: beyaz (beyaz top) + renkli (hedef top).
    // Daireler üst üste binme oranı = çarpışmanın "kalınlığını" gösterir.
    //   Tam üst üste → tam ortadan vurma (4/4)
    //   Kenardan dokunan → ince vurma (1/4)
    //   Ayrı → miss (nişan dışı)
    //
    // Matematiği: CueController nişan doğrusunu hesaplar, buraya (cueBall pozisyonu, yön) gönderir.
    // Biz hedef topa olan dik mesafeyi (perpendicular distance) hesaplar, 0-1 arası kalınlığa çeviririz.
    public class ThicknessIndicator : MonoBehaviour
    {
        // Singleton: ThicknessIndicator.Instance ile her yerden erişilir.
        public static ThicknessIndicator Instance { get; private set; }

        // UI elemanları (UIManager tarafından InitUI ile atanır)
        private RectTransform  _cueBallRt;    // beyaz topun UI dairesi (hareket eder)
        private Image          _targetBallImg; // hedef topun UI dairesi (rengi değişir: sarı/kırmızı)
        private Text           _label;         // "1/4", "1/2" gibi kalınlık etiketi (şu an kullanılmıyor)
        private CanvasGroup    _group;         // tüm panelin görünürlüğü (alpha: 0=gizli, 1=görünür)

        // Hangi topların sarı, hangi topların kırmızı olduğunu bilmek için referanslar
        private BallController _yellowBall;
        private BallController _redBall;

        // Fiziksel ölçüler (BilliardSceneBuilder ile eşleşmeli)
        private const float _ballRadius  = 0.09f;          // top yarıçapı (ölçek 0.18 → yarıçap 0.09)
        private const float _maxContact = _ballRadius * 2f;  // 0.18 = iki top yarıçapı toplamı = temas eşiği
        private const float _showRange  = _ballRadius * 10f; // 0.90 = bu mesafede top göstergesi belirir

        // UIManager tarafından ayarlanır; canvas piksel boyutlarına bağlı.
        // BallDisplayDiameter = UI'daki daire piksel boyutu
        // TargetCenterX = hedef topun container merkezi içindeki X pozisyonu
        public static float BallDisplayDiameter = 100f;
        public static float TargetCenterX       = 50f;

        // UI daire renkleri
        private static readonly Color _yellowColor = new(1.00f, 0.82f, 0.00f, 1f);
        private static readonly Color _redColor    = new(0.88f, 0.12f, 0.12f, 1f);
        private static readonly Color _noneColor   = new(0.55f, 0.55f, 0.55f, 0.6f); // hedef seçilmediğinde

        void Awake() => Instance = this;

        // UIManager çağırır: UI referanslarını atar ve göstergeyi başlangıç durumuna getirir.
        public void InitUI(RectTransform cueBallRt, Image targetBallImg, CanvasGroup group)
        {
            _cueBallRt     = cueBallRt;
            _targetBallImg = targetBallImg;
            _group         = group;
            Apply(null, 0f, false); // başlangıçta gizli
        }

        // TableController.Start() çağırır: sarı ve kırmızı topun referanslarını kaydet.
        public void SetBalls(BallController yellow, BallController red)
        {
            _yellowBall = yellow;
            _redBall    = red;
        }

        // CueController.UpdateVisuals() her kare bu metodu çağırır.
        // cueBallPos = beyaz topun 3D pozisyonu, aimDir = nişan yönü
        // Hangi topa nişan alındığını bulur ve görsel günceller.
        public void UpdateFromAim(Vector3 cueBallPos, Vector3 aimDir)
        {
            if (_cueBallRt == null) return;

            // Her iki top için kalınlık değeri hesapla (0 = ıskalama, 1 = tam ortadan)
            float tY = Thickness(cueBallPos, aimDir, _yellowBall);
            float tR = Thickness(cueBallPos, aimDir, _redBall);
            // Her iki top görünür aralıkta mı?
            bool visY = InShowRange(cueBallPos, aimDir, _yellowBall);
            bool visR = InShowRange(cueBallPos, aimDir, _redBall);

            // En iyi hedefi seç: öncelik yüksek kalınlığa, sonra görünür aralıkta olmasına göre
            BallController target;
            float t;
            if (tY >= tR && tY > 0f)      { target = _yellowBall; t = tY; }
            else if (tR > 0f)             { target = _redBall;    t = tR; }
            else if (visY)                { target = _yellowBall; t = 0f; }
            else if (visR)                { target = _redBall;    t = 0f; }
            else                          { target = null;         t = 0f; }

            Apply(target, t, visY || visR);
        }

        // UI'yı günceller: daire konumunu ve rengini ayarlar, paneli göster/gizle.
        private void Apply(BallController target, float t, bool visible)
        {
            // CanvasGroup.alpha = 0 → tamamen şeffaf (gizli), 1 → tam görünür
            if (_group != null) _group.alpha = visible ? 1f : 0f;

            // Beyaz top dairesinin X pozisyonu:
            //   t=1 (tam isabet) → sep=0  → beyaz top tam üstüne biner (TargetCenterX - 0)
            //   t=0 (ıska)       → sep=D  → beyaz top bir daire kadar solda (sadece kenarlar değer)
            float sep = (1f - t) * BallDisplayDiameter;
            _cueBallRt.anchoredPosition = new Vector2(TargetCenterX - sep, 0f);

            // Hedef topun rengini ayarla (sarı, kırmızı veya gri)
            if (_targetBallImg != null)
                _targetBallImg.color = target == _yellowBall ? _yellowColor :
                                       target == _redBall    ? _redColor    : _noneColor;

            // Kalınlık etiketi (şu an _label null, gelecekte eklenebilir)
            if (_label != null)
                _label.text = t > 0.87f ? "4/4"  :
                              t > 0.62f ? "3/4"  :
                              t > 0.37f ? "1/2"  :
                              t > 0.12f ? "1/4"  :
                              t > 0.01f ? "0/4" : "-";
        }

        // Nişan doğrusu ile hedef top arasındaki dik mesafeyi kalınlık değerine (0-1) çevirir.
        // perp=0 → nişan doğrusu tam top merkezinden geçiyor (kalınlık 1)
        // perp=_maxContact → nişan doğrusu topun kenarına değiyor (kalınlık 0)
        // perp>_maxContact → ıska (kalınlık 0, hesaba katılmaz)
        private float Thickness(Vector3 origin, Vector3 aimDir, BallController ball)
        {
            if (ball == null) return 0f;
            float perp = PerpDist(origin, aimDir, ball);
            if (perp < 0f) return 0f; // top arkada veya çok yanda
            return Mathf.Clamp01(1f - perp / _maxContact);
        }

        // Hedef top nişan doğrusuna görünür mesafede mi?
        // _showRange, _maxContact'tan büyük: topa yaklaşınca gösterge ortaya çıkar.
        private bool InShowRange(Vector3 origin, Vector3 aimDir, BallController ball)
        {
            if (ball == null) return false;
            float perp = PerpDist(origin, aimDir, ball);
            return perp >= 0f && perp < _showRange;
        }

        // Yatay düzlemde, nişan doğrusuna dik mesafeyi hesaplar.
        // Negatif döndürürse: top nişan yönünün arkasında veya çok yanda.
        //
        // Nasıl çalışır:
        //   to = topu gören vektör (origin'den topa)
        //   dir = nişan yönü (normalize edilmiş)
        //   Cross(dir, to).magnitude → dik mesafeyi verir (alan = taban * yükseklik)
        //   Dot(dir, to.normalized) < 0.05 → top neredeyse arkada, geçersiz
        private static float PerpDist(Vector3 origin, Vector3 aimDir, BallController ball)
        {
            if (ball == null) return -1f;
            Vector3 to  = ball.transform.position - origin; to.y = 0f;
            Vector3 dir = new Vector3(aimDir.x, 0f, aimDir.z).normalized;
            if (Vector3.Dot(dir, to.normalized) < 0.05f) return -1f; // top arkada
            return Vector3.Cross(dir, to).magnitude;
        }
    }
}
