using UnityEngine;

namespace Assets.Scripts.Game
{
    // GameMaterials → Sahneye ait materyal (Material) asset'lerini runtime scriptlere sağlar.
    //
    // Neden bu sınıf var?
    //   Unity'de Shader.Find("Universal Render Pipeline/...") runtime'da çalışır,
    //   ama build alındığında bu shader'lar binary'ye paketlenmeyebilir.
    //   Bir materyal bir sahne asset'ine atanmışsa Unity build sırasında o shader'ı dahil eder.
    //   Bu singleton, BilliardSceneBuilder'ın oluşturduğu materyal asset'lerini tutar
    //   ve DiamondMarkers / CueController gibi runtime scriptlere doğrudan verir.
    //   Böylece build'de "shader bulunamadı → pembe/mor hata materyali" sorunu olmaz.
    //
    // [DefaultExecutionOrder(-100)] → Unity'nin script yürütme sırası.
    //   -100 → tüm diğer scriptlerden önce Awake() çalışır.
    //   Sebep: DiamondMarkers ve CueController kendi Awake/Start'larında Instance'a erişir;
    //   bu sınıfın Instance'ı onlardan önce hazır olmalı.
    [DefaultExecutionOrder(-100)]
    public class GameMaterials : MonoBehaviour
    {
        // Singleton: GameMaterials.Instance.Rail gibi erişilir.
        public static GameMaterials Instance { get; private set; }

        void Awake() { Instance = this; }

        // Sahne 3D objeleri için materyal: tahta raylar ve fildişi elmaslar.
        // URP/Unlit → ışık almaz, düz renk görünümü → bilardo masasının düz tahta görünümü için doğru.
        [Header("Scene 3D Materials (URP/Unlit solid colour)")]
        public Material Rail;    // Koyu ceviz rengi, tahta ray görünümü
        public Material Diamond; // Fildişi/sedef rengi, elmas işaretleri

        // LineRenderer materyalleri: nişan çizgisi ve ıstaka çizgisi.
        // URP Particles/Unlit → texture tiling (noktalı desen) ve vertex rengi (baştan sona renk geçişi) destekler.
        [Header("Line Renderer Materials (URP Particles/Unlit — supports texture tiling)")]
        public Material AimLine; // Beyaz noktalı nişan çizgisi
        public Material CueLine; // Kahverengi→açık ıstaka çizgisi
    }
}
