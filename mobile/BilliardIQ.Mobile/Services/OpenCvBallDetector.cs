using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using SDSize  = System.Drawing.Size;
using SDPoint = System.Drawing.Point;

namespace BilliardIQ.Mobile.Services;

/// <summary>
/// OpenCV (EmguCV) kullanarak bilardo toplarını ve masa köşelerini tespit eder.
///
/// Algoritma özeti:
///   1. JPEG → BGR Mat (CvInvoke.Imdecode)
///   2. BGR → HSV renk uzayı (OpenCV HSV: H=0-180, S=0-255, V=0-255)
///   3. Her top rengi için InRange ile ikili maske oluştur
///   4. GaussianBlur ile gürültü azalt
///   5. HoughCircles ile dairesel yapıları bul → en iyi daire = top konumu
///   6. Masa köşeleri: yeşil maske → MorphologyEx → findContours → ApproxPolyDP (4 köşe)
///   7. Sonuçlar SkiaSharp ile orijinal görüntüye çizilir
///
/// HoughCircles vs blob tespiti:
///   Blob tespiti en büyük ya da en dairesel renk bölgesini alır.
///   HoughCircles ise görüntü gradyanını (kenarları) kullanarak akümülatör
///   oylama sistemiyle daire merkezleri ve yarıçapları bulur.
///   Sonuç: HoughCircles daha doğru merkez tespiti sağlar ama parametre ayarı önemlidir.
/// </summary>
public class OpenCvBallDetector
{
    private const int ScaleDim = 640;

    // Native lib availability check — runs once, before any CvInvoke call
    private static readonly bool _nativeAvailable = CheckNative();
    private static bool CheckNative()
    {
        try { CvInvoke.Init(); return true; }
        catch { return false; }
    }

    /// <summary>
    /// Verilen JPEG baytlarını analiz eder.
    /// Dönen koordinatlar orijinal görüntü piksel cinsinden ve 0-1 relative olarak verilir.
    /// </summary>
    public (List<TableBall> Balls, List<(float X, float Y)> Corners) Detect(byte[] jpegBytes)
    {
        if (!_nativeAvailable) return ([], []);

        // JPEG'i doğrudan BGR Mat olarak yükle
        using var bgr = new Mat();
        CvInvoke.Imdecode(jpegBytes, ImreadModes.ColorBgr, bgr);
        if (bgr.IsEmpty) return ([], []);

        int origW = bgr.Width, origH = bgr.Height;

        // Analiz için büyük kenara göre 640px'e ölçekle
        double scaleF = Math.Min((double)ScaleDim / Math.Max(origW, origH), 1.0);
        int sw = Math.Max(1, (int)(origW * scaleF));
        int sh = Math.Max(1, (int)(origH * scaleF));

        using var small = new Mat();
        CvInvoke.Resize(bgr, small, new SDSize(sw, sh));

        // BGR → HSV (OpenCV HSV: H=0-180° aralığında — gerçek H/2 değeri)
        using var hsv = new Mat();
        CvInvoke.CvtColor(small, hsv, ColorConversion.Bgr2Hsv);

        // ── Renk maskeleri ──────────────────────────────────────────────────
        // Beyaz: düşük doygunluk, yüksek parlaklık (H herhangi, S=0-55, V=175-255)
        using var whiteMask = Threshold(hsv, new MCvScalar(0, 0, 175), new MCvScalar(180, 55, 255));

        // Sarı: H=8-38 (≈16-76° gerçek), yüksek S ve V
        using var yellowMask = Threshold(hsv, new MCvScalar(8, 80, 80), new MCvScalar(38, 255, 255));

        // Kırmızı: iki aralık — H=0-10 ve H=168-180 (kırmızı renk çemberinin iki yanı)
        using var red1 = Threshold(hsv, new MCvScalar(0,   80, 50), new MCvScalar(10,  255, 255));
        using var red2 = Threshold(hsv, new MCvScalar(168, 80, 50), new MCvScalar(180, 255, 255));
        using var redMask = new Mat();
        CvInvoke.Add(red1, red2, redMask);

        // Yeşil (masa örtüsü): H=35-90, orta-yüksek S ve V
        using var greenMask = Threshold(hsv, new MCvScalar(35, 25, 18), new MCvScalar(92, 255, 255));

        // ── Top tespiti ─────────────────────────────────────────────────────
        // Top büyüklük aralığı: masanın yaklaşık %0.7-12'si kadar piksel
        int minR = Math.Max(4, (int)(Math.Max(sw, sh) * 0.007));
        int maxR = (int)(Math.Max(sw, sh) * 0.12);

        var balls = new List<TableBall>(3);
        FindBall(whiteMask,  BallColor.White,  sw, sh, minR, maxR, scaleF, origW, origH, balls);
        FindBall(yellowMask, BallColor.Yellow, sw, sh, minR, maxR, scaleF, origW, origH, balls);
        FindBall(redMask,    BallColor.Red,    sw, sh, minR, maxR, scaleF, origW, origH, balls);

        // ── Köşe tespiti ────────────────────────────────────────────────────
        var corners = FindTableCorners(greenMask, sw, sh, scaleF);

        return (balls, corners);
    }

    // ── HoughCircles ile tek top bul ──────────────────────────────────────

    /// <summary>
    /// Renk maskesine HoughCircles uygular, bulunan ilk (en yüksek oy alan) daireyi top olarak döndürür.
    ///
    /// HoughCircles parametreleri:
    ///   dp=1.5       → akümülatör çözünürlüğü (1=giriş, 2=yarı)
    ///   minDist      → iki daire merkezi arasındaki minimum mesafe (çakışmayı önler)
    ///   param1=100   → Canny edge'in üst eşiği (iç ön işlemede kullanılır)
    ///   param2=12    → akümülatör eşiği (düşük=daha fazla daire, yanlış pozitif riski artar)
    /// </summary>
    private static void FindBall(
        Mat mask, BallColor color,
        int sw, int sh, int minR, int maxR,
        double scaleF, int origW, int origH,
        List<TableBall> results)
    {
        // GaussianBlur: HoughCircles gürültüye duyarlıdır; küçük nokta gürültüsü temizlenir
        using var blurred = new Mat();
        CvInvoke.GaussianBlur(mask, blurred, new SDSize(7, 7), 2.0);

        // HoughCircles: ikili maske üzerinde çevre tespiti + Hough oylama
        CircleF[] circles = CvInvoke.HoughCircles(
            blurred,
            HoughModes.Gradient,
            dp:        1.5,
            minDist:   minR * 3.0,   // en az 3 yarıçap arası boşluk
            param1:    100,
            param2:    12,            // eşik: çok düşük olursa yanlış pozitif çoğalır
            minRadius: minR,
            maxRadius: maxR);

        if (circles.Length == 0) return;

        // HoughCircles zaten oy sayısına göre sıralı döner → ilk = en güvenilir
        var c = circles[0];
        float cx = (float)(c.Center.X / scaleF / origW);   // 0-1 relative
        float cy = (float)(c.Center.Y / scaleF / origH);
        float r  = (float)(c.Radius   / scaleF / Math.Max(origW, origH));

        results.Add(new TableBall(
            color, cx, cy, r,
            (int)(c.Center.X / scaleF),
            (int)(c.Center.Y / scaleF),
            (int)(c.Radius   / scaleF)));
    }

    // ── findContours + ApproxPolyDP ile masa köşeleri ─────────────────────

    /// <summary>
    /// Yeşil maskeden masanın 4 köşesini bulur.
    ///
    /// Adımlar:
    ///   1. MorphologyEx (Close): küçük delikleri ve kopuklukları kapatır
    ///   2. findContours: dış konturları bulur
    ///   3. En büyük kontur = masa
    ///   4. ApproxPolyDP (Douglas-Peucker): kontur noktalarını sadeleştirir
    ///      epsilon = perimeter * 0.02 → %2 tolerans
    ///   5. 4 noktalı poligon → köşeler TL, TR, BR, BL sırasına sokulur
    ///      Sıralama: min(x+y)=TL, min(y-x)=TR, max(x+y)=BR, max(y-x)=BL
    /// </summary>
    private static List<(float X, float Y)> FindTableCorners(
        Mat greenMask, int sw, int sh, double scaleF)
    {
        // Morfolojik kapama: maskenin içindeki delikleri kapat (top gölgeleri, yansımalar)
        using var kernel  = CvInvoke.GetStructuringElement(
            MorphShapes.Ellipse, new SDSize(7, 7), new SDPoint(-1, -1));
        using var closed = new Mat();
        CvInvoke.MorphologyEx(greenMask, closed, MorphOp.Close,
            kernel, new SDPoint(-1, -1), 2, BorderType.Default, new MCvScalar());

        // Dış konturları bul
        using var contours  = new VectorOfVectorOfPoint();
        using var hierarchy = new Mat();
        CvInvoke.FindContours(closed, contours, hierarchy,
            RetrType.External, ChainApproxMethod.ChainApproxSimple);

        if (contours.Size == 0) return [];

        // En büyük kontur = masa (en fazla alana sahip)
        int bestIdx = 0; double bestArea = 0;
        for (int i = 0; i < contours.Size; i++)
        {
            double a = CvInvoke.ContourArea(contours[i]);
            if (a > bestArea) { bestArea = a; bestIdx = i; }
        }

        // Masa görüntünün en az %5'ini kaplamalı
        if (bestArea < sw * sh * 0.05) return [];

        // Douglas-Peucker ile kontur sadeleştirme → 4 köşe poligonu
        using var approx = new VectorOfPoint();
        double perimeter = CvInvoke.ArcLength(contours[bestIdx], true);
        double epsilon   = 0.02 * perimeter;
        CvInvoke.ApproxPolyDP(contours[bestIdx], approx, epsilon, true);

        // 4 noktaya ulaşamadıysa epsilon'u değiştirerek tekrar dene
        if (approx.Size != 4)
        {
            CvInvoke.ApproxPolyDP(contours[bestIdx], approx, 0.04 * perimeter, true);
        }
        if (approx.Size != 4) return [];

        // Köşeleri TL, TR, BR, BL sırasına sok
        var pts = approx.ToArray();
        var tl = pts.OrderBy(p => p.X + p.Y).First();       // min(x+y) = sol-üst
        var br = pts.OrderByDescending(p => p.X + p.Y).First(); // max(x+y) = sağ-alt
        var tr = pts.OrderBy(p => p.Y - p.X).First();       // min(y-x) = sağ-üst
        var bl = pts.OrderByDescending(p => p.Y - p.X).First(); // max(y-x) = sol-alt

        float inv = (float)(1.0 / scaleF);
        return
        [
            (tl.X * inv, tl.Y * inv),
            (tr.X * inv, tr.Y * inv),
            (br.X * inv, br.Y * inv),
            (bl.X * inv, bl.Y * inv),
        ];
    }

    // ── Yardımcı: InRange mask ────────────────────────────────────────────

    /// <summary>
    /// HSV görüntüsünde belirli renk aralığındaki pikselleri beyaz, diğerlerini siyah yapan maske.
    /// OpenCV InRange: koşul sağlanıyorsa 255, sağlanmıyorsa 0.
    /// </summary>
    private static Mat Threshold(Mat hsv, MCvScalar low, MCvScalar high)
    {
        var mask = new Mat();
        CvInvoke.InRange(hsv,
            new ScalarArray(low),
            new ScalarArray(high),
            mask);
        return mask;
    }
}
