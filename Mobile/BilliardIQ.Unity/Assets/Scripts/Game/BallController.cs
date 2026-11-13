using UnityEngine;

namespace Assets.Scripts.Game
{
    // Her topun sahne üzerindeki rolünü tanımlar.
    // CueBall = oyuncunun vurduğu beyaz top
    // YellowBall / RedBall = hedef toplar (3-bant bilardoda vurulması gereken toplar)
    public enum BallRole { CueBall, YellowBall, RedBall }

    // [RequireComponent] → Unity editörü bu script bir objeye eklendiğinde,
    // Rigidbody bileşenini de otomatik olarak ekler. Rigidbody olmadan fizik çalışmaz.
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        // Bu topun rolü (beyaz mı, sarı mı, kırmızı mı?). Inspector'dan atanır.
        public BallRole Role;

        // Rigidbody → Unity'nin fizik motoru. Hız, kütle, yerçekimi, çarpışma hepsi buradan yönetilir.
        private Rigidbody _rb;

        // Atış aktif mi? Aktif değilken çarpışma olayları görmezden gelinir.
        // Örneğin toplar hareket ederken sahne yeniden başlatılırsa yanlış sayım olmaması için.
        private bool _shotActive;

        // Atış anında kaydedilen spin (döndürme) bilgisi.
        // x = yan spin (İngiliz): -1 sola, +1 sağa
        // y = üst/alt spin: -1 çekme (draw), +1 pika (follow)
        private Vector2 _spin = Vector2.zero;

        // Aynı kareye birden fazla bant çarpışması kaydetmemek için kullanılır.
        // frameCount ile karşılaştırarak "bu kare zaten kaydettim" diye kontrol ederiz.
        private int _lastCushionHitFrame = -10;

        // Oyun alanının X ekseni sınırı: masa yarı genişliği (2.81) - top yarıçapı (0.09) = 2.72
        // Top bu sınırın dışına çıkarsa duvara girmiş gibi görünür, bu yüzden geri iter.
        private const float _maxX = 2.72f;

        // Oyun alanının Z ekseni sınırı: masa yarı yüksekliği (1.38) - top yarıçapı (0.09) = 1.29
        private const float _maxZ = 1.29f;

        // Bant çarpışmasında yan spin'in hıza ne kadar etki edeceğini belirler.
        // 1.2 = biraz abartılmış, görsel olarak daha belirgin hissettirmek için.
        private const float _englishFactor = 1.2f;

        // Awake() → Unity'nin en erken çağırdığı yaşam döngüsü metodudur.
        // Start()'tan önce çalışır. Bileşen başlatma işlemleri burada yapılır.
        void Awake()
        {
            // Bu obje üzerindeki Rigidbody bileşenini al ve _rb değişkenine kaydet
            _rb = GetComponent<Rigidbody>();

            // linearDamping → hareket sürtünmesi: her saniye hız bu oranda azalır (üstel).
            // 0.4 → çok az sürtünme, top uzun süre hızlı gidiyordu.
            // 0.8 → daha gerçekçi: top yavaşça durur (~3-4 saniyede).
            _rb.linearDamping  = 0.8f;
            _rb.angularDamping = 0.8f;

            // maxAngularVelocity → topun döneceği maksimum açısal hız.
            // Unity'nin varsayılanı çok düşük olduğundan spin efektleri görünmez hale gelir.
            _rb.maxAngularVelocity = 20f;

            // ContinuousDynamic → Unity'nin en hassas çarpışma tespit modu.
            // Hızlı hareket eden toplar "tünel etkisi" ile hedef topu delip geçmesin diye.
            // Discrete (varsayılan) → her kare kontrol eder, hızlı toplar atlanabilir.
            // ContinuousDynamic → sürekli kontrol eder, atlama olmaz.
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // RigidbodyConstraints → fizik motorunun hangi eksenlerde hareket etmesine izin verilir.
            // Bilardo topu masa yüzeyinde hareket eder, Y ekseninde yukarı çıkmamalı.
            // X ve Z dönüşleri de kilitli: top yuvarlanmaz, sadece sürtünmeyle yavaşlar.
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezeRotationX
                            | RigidbodyConstraints.FreezeRotationZ;
        }

        // Atışın aktif olup olmadığını dışarıdan ayarlamak için kullanılır.
        // TableController çarpışma olaylarının sayılıp sayılmayacağını bu flag ile kontrol eder.
        public void SetShotActive(bool active) => _shotActive = active;

        // Topun hareket edip etmediğini kontrol eder.
        // linearVelocity.magnitude → hız vektörünün büyüklüğü (m/s).
        // 0.05'ten küçükse "durdu" sayarız. Tam sıfır beklemek sonsuz döngüye yol açabilir.
        public bool IsMoving() => _rb.linearVelocity.magnitude > 0.05f;

        // Topa kuvvet uygular ve spin kaydeder.
        // force: yön * güç vektörü (CueController tarafından hesaplanır)
        // spin: atış noktası seçiciden gelen döndürme değerleri
        public void ApplyForce(Vector3 force, Vector2 spin = default)
        {
            // Önceki hızı sıfırla; aksi takdirde kuvvetler birikerek tahmin edilemez harekete yol açar
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _spin = spin;

            // ForceMode.Impulse → anlık kuvvet uygular (F = kütle * ivme, tek seferde).
            // Force modu her frame'de kuvvet ekler; Impulse tek seferlik vurma için doğrudur.
            _rb.AddForce(force, ForceMode.Impulse);

            // Yan spin varsa topu Y ekseni etrafında döndür (görsel efekt).
            // Fizik kısıtları X/Z dönüşünü engellediğinden sadece Y'de görünür dönüş yapabiliriz.
            if (Mathf.Abs(spin.x) > 0.05f)
                _rb.angularVelocity = new Vector3(0f, -spin.x * 10f, 0f);
        }

        // Topu anında durdurur. Atış bitmeden sahne sıfırlanacaksa çağrılır.
        public void Stop()
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _spin = Vector2.zero;
        }

        // FixedUpdate() → fizik adımlarıyla senkron çalışır.
        // Backspin/topspin etkisini top hareket ettiği sürece sürekli uygular.
        // Gerçek bilardoda spin, topun tüm hareketi boyunca zemin sürtünmesiyle etkileşir.
        void FixedUpdate()
        {
            if (!_shotActive || Mathf.Abs(_spin.y) < 0.05f) return;
            float speed = _rb.linearVelocity.magnitude;
            if (speed < 0.05f) return;

            // Bant yakınındayken spin kuvveti uygulanmaz: aksi halde top bant boyunca kayar.
            Vector3 p = _rb.position;
            if (Mathf.Abs(p.x) > _maxX - 0.15f || Mathf.Abs(p.z) > _maxZ - 0.15f) return;

            Vector3 dir = _rb.linearVelocity / speed;
            float scale = Mathf.Clamp01(speed / 1.0f);
            _rb.AddForce(dir * _spin.y * 1.2f * scale, ForceMode.Acceleration);
        }

        // LateUpdate() → Update()'ten sonra, her kare en sonda çalışır.
        // Fizik hesapları Update'te yapılır; biz sonuç üzerinde düzeltme yaptığımız için LateUpdate kullanıyoruz.
        // Sınır kontrolü: top bant dışına çıkarsa geri it ve hızını ters çevir.
        void LateUpdate()
        {
            if (_rb == null) return;
            Vector3 p = _rb.position;
            Vector3 v = _rb.linearVelocity;
            bool clamped = false;

            // X ekseni sınır aşımı: pozisyonu sınıra kilitle, hızın X bileşenini ters çevir
            // * 0.7f → %30 enerji kaybı (bant esnekliği simülasyonu)
            if (Mathf.Abs(p.x) > _maxX) { p.x = Mathf.Sign(p.x) * _maxX; v.x = -v.x * 0.7f; clamped = true; }
            if (Mathf.Abs(p.z) > _maxZ) { p.z = Mathf.Sign(p.z) * _maxZ; v.z = -v.z * 0.7f; clamped = true; }

            // MovePosition → fizik motoruna pozisyonu doğrudan atamak yerine "buraya taşı" der.
            // Doğrudan transform.position atamak fizik hesaplarını bozabilir.
            if (clamped) { _rb.MovePosition(p); _rb.linearVelocity = v; }
        }

        // OnCollisionEnter() → bu obje başka bir objeyle çarpıştığında Unity tarafından otomatik çağrılır.
        // Collision parametresi: çarpışan obje, temas noktaları, çarpışma kuvveti gibi bilgileri içerir.
        void OnCollisionEnter(Collision col)
        {
            // Atış aktif değilse (örn. toplar henüz hareket etmiyorsa) işlem yapma
            if (!_shotActive) return;

            // Sadece beyaz top (CueBall) bantla (Cushion tag'li objelerle) çarpışınca işlem yap
            if (Role == BallRole.CueBall && col.gameObject.CompareTag("Cushion"))
            {
                // Çarpışma sonrası yan spin etkisini uygula
                ApplyEnglishOnCushion(col);

                // Aynı bant çarpışmasını birden fazla kez sayma.
                // Bir bant teması birkaç fizik karesi sürebilir; sadece ilkini kaydet.
                if (Time.frameCount - _lastCushionHitFrame > 3)
                {
                    _lastCushionHitFrame = Time.frameCount;
                    if(GameController.Instance != null)
                    GameController.Instance?.RegisterCushionHit();
                }
            }
        }

        // OnCollisionExit() → bu obje bir objeyle temasını kestiğinde Unity tarafından otomatik çağrılır.
        // "Çarpışmadan çıkarken" üst/alt spin etkisini uygularız, çünkü
        // henüz çarpışma sürerken hız değişimi tuhaf sonuçlar verir.
        void OnCollisionExit(Collision col)
        {
            if (!_shotActive || Role != BallRole.CueBall) return;

            // Çarpışılan objenin BallController bileşeni var mı? (top mu yoksa bant mı?)
            var other = col.gameObject.GetComponent<BallController>();
            if (other == null || other.Role == BallRole.CueBall) return; // top-top değilse çık

            // Beyaz top bir hedef topla çarpıştıktan sonra üst/alt spin etkisi uygula
            ApplyTopBottomSpinAfterBallHit();
        }

        // Bant çarpışmasında yan spin (İngiliz) etkisi.
        // Sağa spin + sağ bant = "running" (açı genişler).
        // Sola spin + sağ bant = "check" (açı daralır).
        private void ApplyEnglishOnCushion(Collision col)
        {
            // Spin çok küçükse veya temas noktası yoksa işlem yapma
            if (Mathf.Abs(_spin.x) < 0.05f || col.contactCount == 0) return;

            // Temas noktasının normal vektörü: banttan içe doğru bakan birim vektör
            Vector3 normal = col.contacts[0].normal;
            normal.y = 0f; // Y bileşenini sıfırla; sadece yatay düzlemde çalışıyoruz
            if (normal.sqrMagnitude < 0.001f) return;
            normal.Normalize();

            // Bant yüzeyine paralel yön: normal'e dik açıyla, yatay düzlemde
            // Cross(up, normal) → sağ el kuralıyla bant yüzeyine paralel vektör verir
            Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;

            // Hıza tanjant yönünde spin * katsayı kadar ekle
            // Spin.x > 0 (sağ spin): topa tanjant yönünde ivme katar → açı genişler
            _rb.linearVelocity += tangent * _spin.x * _englishFactor;
        }

        // Top-top çarpışmasından sonra üst/alt spin etkisi.
        // Follow (pika, y > 0): beyaz top çarpıştıktan sonra ileri devam eder.
        // Draw (çekme, y < 0): beyaz top yavaşlar veya geri döner.
        //
        // Eski versiyon sabit kuvvet kullanıyordu (1.5f), topun hızından bağımsızdı.
        // Yüksek hızda top hâlâ 15 m/s gidiyorken 1.5f impulse hiç fark etmiyordu.
        // Yeni versiyon ForceMode.VelocityChange kullanır: kütle bağımsız, doğrudan hıza eklenir.
        private void ApplyTopBottomSpinAfterBallHit()
        {
            if (Mathf.Abs(_spin.y) < 0.05f) return;

            float speed = _rb.linearVelocity.magnitude;
            Vector3 vel = _rb.linearVelocity;
            // Yönü önce kaydet: aşağıda velocty'yi değiştireceğiz, dir kayması olmasın.
            Vector3 dir = speed > 0.001f ? vel / speed : Vector3.zero;

            if (_spin.y > 0f) // Pika / follow: ileri devam
            {
                if (speed < 0.05f) return; // top zaten durmuş, pika etkisi yok
                // Mevcut hıza spin oranıyla orantılı ileri momentum ekle.
                // 0.8f katsayısı: tam pika (spin.y=1) → hız %80 artar.
                _rb.linearVelocity = vel + _spin.y * 0.8f * speed * dir;
            }
            else // Çekme / draw: yavaşla veya geri dön
            {
                float draw = Mathf.Abs(_spin.y);

                // Adım 1: mevcut ileri momentumu büyük oranda söndür.
                // draw=1 (tam çekme) → hız sıfıra iner; draw=0.5 → %50 azalır.
                _rb.linearVelocity = vel * (1f - draw);

                // Adım 2: geri yönde doğrudan hız ekle (ForceMode.VelocityChange = kütleden bağımsız).
                // draw=1 → 2.5 m/s geri; draw=0.5 → 1.25 m/s geri.
                // Sabit olduğu için düşük hızda da yüksek hızda da aynı geriye çekme hissi verir.
                _rb.AddForce(-dir * draw * 2.5f, ForceMode.VelocityChange);
            }
        }
    }
}
