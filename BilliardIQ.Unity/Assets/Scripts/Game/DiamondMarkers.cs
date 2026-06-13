using UnityEngine;

namespace Assets.Scripts.Game
{
    // DiamondMarkers → masanın tahta raylarını (bantların dışındaki çerçeve) ve
    // ray üzerindeki elmas işaretlerini (bilardo oyuncularının açı hesaplamak için kullandığı noktalar) oluşturur.
    //
    // Çalışma mantığı:
    //   1. "Cushion" tag'li mevcut fizik collider'larının görsel (Renderer) kısmını gizler.
    //   2. Yerlerine tahta görünümlü Cube objelerini raylar olarak ekler (collider'sız, sadece görsel).
    //   3. Ray üzerine düz silindir (disk) olarak elmas işaretleri yerleştirir.
    //
    // Gerçek ölçekler (2:1 → 284cm = 5.68 Unity birimi):
    //   İç alan: 284 × 142 cm  →  5.68 × 2.84 Unity birimi
    //   Ray genişliği: 12 cm   →  0.24 Unity birimi
    //   Ray yüksekliği: 8 cm   →  0.16 Unity birimi
    public class DiamondMarkers : MonoBehaviour
    {
        // İç oyun alanının yarı boyutları (masanın ortasından kenara mesafe)
        private const float _innerHalfX = 2.84f;
        private const float _innerHalfZ = 1.42f;

        // Ray geometrisi
        private const float _railWidth  = 0.24f;
        private const float _railHeight = 0.16f;

        // Uzun rayların merkezi: iç alan Z sınırının hemen dışı
        private const float _longRailCenterZ  = _innerHalfZ + _railWidth / 2;
        // Kısa rayların merkezi: iç alan X sınırının hemen dışı
        private const float _shortRailCenterX = _innerHalfX + _railWidth / 2;
        // Uzun ray toplam X genişliği: iç alan + iki köşe (köşeler uzun raylara dahil)
        private const float _longRailSpanX    = (_innerHalfX + _railWidth) * 2;
        // Kısa ray Z uzunluğu: sadece iç alan (köşeler uzun raylara ait)
        private const float _shortRailSpanZ   = _innerHalfZ * 2;

        // Ray Y konumu: masa yüzeyi (0.05) + ray yüksekliğinin yarısı
        private const float _railCenterY = 0.13f;

        // Elmas adımı: 5.68 / 8 = 0.71 Unity birimi = 35.5 cm gerçek
        // Standart 3-bant bilardo masası: uzun rayta 8 eşit bölüm = 7 iç elmas
        //                                 kısa rayta 4 eşit bölüm = 3 iç elmas
        private const float _diamondStep = 0.71f;
        private const float _diamondY  = 0.25f;  // ray üstünden biraz yüksekte (iyi görünür)

        // Elmas disk boyutu: çok küçük Y ölçeği → yassı disk görünümü
        private static readonly Vector3 _diamondScale = new(0.13f, 0.018f, 0.13f);

        // Renk sabitleri
        private static readonly Color _railColor    = new(0.20f, 0.13f, 0.05f, 1f); // koyu ceviz/walnut
        private static readonly Color _diamondColor = new(1.00f, 0.97f, 0.88f, 1f); // fildişi/sedef

        // Start() → TableController.Start() bu bileşeni AddComponent ile ekler; hemen sonra Start çağrılır.
        void Start()
        {
            // Mevcut bant objelerinin görselini gizle (Renderer disabled), fizik collider'ları aktif kalır.
            // Böylece elmas ve ray görünümü bant fizik davranışını etkilemez.
            foreach (var go in GameObject.FindGameObjectsWithTag("Cushion"))
            {
                if (go.TryGetComponent<Renderer>(out var r)) r.enabled = false;
            }

            // Tüm ray ve elmas objelerini bir parent altında topla (Inspector'u temiz tutar)
            var parent = new GameObject("TableRails");
            parent.transform.SetParent(transform, false);

            BuildRails(parent);
            BuildDiamonds(parent);
        }

        // Dört taraf ray objesini oluşturur: 2 uzun (N/S), 2 kısa (E/W).
        private static void BuildRails(GameObject parent)
        {
            var mat = GetMaterial(GameMaterials.Instance?.Rail, _railColor);

            // Uzun ray (yakın, +Z tarafı): masanın yukarısındaki uzun kenar
            CreateRailPiece(parent, "Rail_LongNear",
                new Vector3(0f, _railCenterY,  _longRailCenterZ),
                new Vector3(_longRailSpanX, _railHeight, _railWidth), mat);

            // Uzun ray (uzak, -Z tarafı): masanın aşağısındaki uzun kenar
            CreateRailPiece(parent, "Rail_LongFar",
                new Vector3(0f, _railCenterY, -_longRailCenterZ),
                new Vector3(_longRailSpanX, _railHeight, _railWidth), mat);

            // Kısa ray (sağ, +X): masanın sağ kısa kenarı (köşeler uzun raylara dahil olduğu için kısa)
            CreateRailPiece(parent, "Rail_ShortRight",
                new Vector3( _shortRailCenterX, _railCenterY, 0f),
                new Vector3(_railWidth, _railHeight, _shortRailSpanZ), mat);

            // Kısa ray (sol, -X): masanın sol kısa kenarı
            CreateRailPiece(parent, "Rail_ShortLeft",
                new Vector3(-_shortRailCenterX, _railCenterY, 0f),
                new Vector3(_railWidth, _railHeight, _shortRailSpanZ), mat);
        }

        // Tek bir ray parçası oluşturur: sadece görsel Cube (BoxCollider olmadan).
        // Fizik: mevcut Cushion collider'ları zaten orada, yeni collider istemiyoruz.
        private static void CreateRailPiece(GameObject parent, string name,
            Vector3 pos, Vector3 scale, Material mat)
        {
            // CreatePrimitive(Cube) → hem Renderer hem BoxCollider ile bir küp oluşturur
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position   = pos;
            go.transform.localScale = scale;

            var r = go.GetComponent<Renderer>();
            r.material             = mat;
            r.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off; // gölge performansı için kapalı
            r.receiveShadows       = false;

            // BoxCollider'ı kaldır: fizik bantlar (Cushion collider'lar) zaten var, çift collider çakışır.
            Destroy(go.GetComponent<BoxCollider>());
        }

        // Tüm ray üzerindeki elmas işaretlerini yerleştirir.
        private static void BuildDiamonds(GameObject parent)
        {
            var mat = GetMaterial(GameMaterials.Instance?.Diamond, _diamondColor);

            // Uzun raylarda 7 elmas: i=1..7 → X pozisyonu = (i-4) * adım
            // Merkez (i=4) X=0, solda 3 elmas (negatif), sağda 3 elmas (pozitif)
            for (int i = 1; i <= 7; i++)
            {
                float x = (i - 4) * _diamondStep;
                PlaceDiamond(parent, new Vector3(x, _diamondY,  _longRailCenterZ), mat); // yakın ray
                PlaceDiamond(parent, new Vector3(x, _diamondY, -_longRailCenterZ), mat); // uzak ray
            }

            // Kısa raylarda 3 elmas: j=1..3 → Z pozisyonu = (j-2) * adım
            for (int j = 1; j <= 3; j++)
            {
                float z = (j - 2) * _diamondStep;
                PlaceDiamond(parent, new Vector3( _shortRailCenterX, _diamondY, z), mat); // sağ ray
                PlaceDiamond(parent, new Vector3(-_shortRailCenterX, _diamondY, z), mat); // sol ray
            }
        }

        // Tek bir elmas (yassı disk) oluşturur.
        // Cylinder primitive → CapsuleCollider içerir; fizik istemiyoruz, Destroy ile kaldırılır.
        // Çok küçük Y ölçeği (0.018) → yassı disk görünümü (elmas işareti)
        private static void PlaceDiamond(GameObject parent, Vector3 pos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent.transform, false);
            go.transform.position   = pos;
            go.transform.localScale = _diamondScale;

            var r = go.GetComponent<Renderer>();
            r.material          = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;

            // CapsuleCollider → Cylinder primitive'de otomatik eklenir; fizik istemiyoruz.
            Destroy(go.GetComponent<CapsuleCollider>());
        }

        // GameMaterials singleton'ından materyali alır.
        // GameMaterials yoksa (sahne yeniden kurulmadıysa) runtime'da yeni materyal oluşturur.
        // Build'de shader'ın paketlenmiş olması için asset materyali tercih edilir;
        // Shader.Find() runtime'da paketlenmemiş shader'ları bulamaz.
        private static Material GetMaterial(Material assetMat, Color color)
        {
            if (assetMat != null) return assetMat;
            var sh = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Sprites/Default");
            return new Material(sh) { color = color };
        }
    }
}
