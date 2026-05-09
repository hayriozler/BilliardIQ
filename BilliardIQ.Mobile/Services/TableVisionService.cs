using SkiaSharp;

namespace BilliardIQ.Mobile.Services;

/// <summary>Bilardo masası fotoğrafından elde edilen top ve köşe bilgileri.</summary>
/// <param name="Color">Topun rengi.</param>
/// <param name="CenterX">Görüntü genişliğine göre yatay merkez (0–1).</param>
/// <param name="CenterY">Görüntü yüksekliğine göre dikey merkez (0–1).</param>
/// <param name="Radius">Büyük boyuta (width/height max) göre yarıçap (0–1).</param>
public record TableBall(BallColor Color, float CenterX, float CenterY, float Radius);

/// <summary>Masa analizi sonucu.</summary>
public class TableAnalysisResult
{
    public IReadOnlyList<TableBall> Balls { get; init; } = [];
    /// <summary>Dört köşe: Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt (piksel).</summary>
    public IReadOnlyList<(float X, float Y)> Corners { get; init; } = [];
    public byte[] AnnotatedImage { get; init; } = [];
    public string StatusMessage { get; init; } = string.Empty;
}

/// <summary>
/// Bilardo masası fotoğrafından top ve masa köşelerini tespit eder.
///
/// Çalışma adımları:
///   1. Görüntü analiz için 640px'e ölçeklenir (performans).
///   2. Her piksel HSV rengine çevrilir; yeşil (masa örtüsü), beyaz/sarı/kırmızı maskeleri oluşturulur.
///   3. Masa köşeleri: yeşil bölgenin köşegen-ekstrem noktaları (Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt).
///   4. Toplar: önce ONNX modeli denenir; model yoksa her renk maskesinde BFS flood-fill ile en büyük
///      bağlı bileşen bulunur (1 beyaz, 1 sarı, 1 kırmızı).
///   5. Tespit sonuçları orijinal görüntü üzerine çizilir ve JPEG olarak döndürülür.
/// </summary>
public class TableVisionService(BallDetectionService onnxDetector)
{
    private const int WorkDim = 640; // analiz için hedef boyut

    public async Task<TableAnalysisResult> AnalyzeAsync(string imagePath)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath);
        return await AnalyzeBytesAsync(bytes);
    }

    public async Task<TableAnalysisResult> AnalyzeBytesAsync(byte[] imageBytes)
    {
        // ONNX modeli varsa onu kullan (daha hızlı ve doğru)
        var onnxBalls = await onnxDetector.DetectAsync(imageBytes);
        return await Task.Run(() => RunAnalysis(imageBytes, onnxBalls));
    }

    // ── Çekirdek analiz ───────────────────────────────────────────────────────

    private static TableAnalysisResult RunAnalysis(byte[] imageBytes, IReadOnlyList<DetectedBall> onnxBalls)
    {
        using var orig = SKBitmap.Decode(imageBytes);
        if (orig is null)
            return new TableAnalysisResult { StatusMessage = "Görüntü okunamadı." };

        int ow = orig.Width, oh = orig.Height;

        // Büyük kenara göre WorkDim'e kadar küçült (upsample yapma)
        float scale = (float)WorkDim / Math.Max(ow, oh);
        scale = Math.Min(scale, 1f);
        int ww = Math.Max(1, (int)(ow * scale));
        int wh = Math.Max(1, (int)(oh * scale));

        using var work = ScaleBitmap(orig, ww, wh);

        // HSV renk maskeleri
        var (greenMask, whiteMask, yellowMask, redMask) = BuildMasks(work, ww, wh);

        // Masa köşeleri (yeşil bölgenin ekstrem noktaları)
        var corners = FindCorners(greenMask, ww, wh, scale);

        // Top tespiti
        List<TableBall> balls;
        if (onnxBalls.Count > 0)
        {
            // ONNX modeli: relative [0,1] koordinatları kullan
            int tableW = EstimateTableWidth(greenMask, ww, wh);
            balls = [..onnxBalls.Select(b =>
            {
                float r = tableW * 0.021f / Math.Max(ww, wh); // toplarda top/tablo ~%2.1
                return new TableBall(b.Color, b.CenterX, b.CenterY, r);
            })];
        }
        else
        {
            // Fallback: renk segmentasyonu
            balls = DetectByColor(whiteMask, yellowMask, redMask, ww, wh);
        }

        // Orijinal görüntüye bindirme çiz
        byte[] annotated = DrawAnnotations(orig, balls, corners);

        return new TableAnalysisResult
        {
            Balls          = balls,
            Corners        = corners,
            AnnotatedImage = annotated,
            StatusMessage  = BuildStatus(balls, corners)
        };
    }

    // ── Renk maskeleri ────────────────────────────────────────────────────────

    private static (bool[] green, bool[] white, bool[] yellow, bool[] red)
        BuildMasks(SKBitmap bmp, int w, int h)
    {
        var green  = new bool[w * h];
        var white  = new bool[w * h];
        var yellow = new bool[w * h];
        var red    = new bool[w * h];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var px  = bmp.GetPixel(x, y);
            int idx = y * w + x;
            RgbToHsv(px.Red, px.Green, px.Blue, out float hue, out float sat, out float val);

            // Yeşil (masa örtüsü): tipik bilardo keçesi rengi H=80-175, doygun, orta parlak
            green[idx] = hue is >= 70f and <= 180f && sat > 0.22f && val > 0.12f;

            if (green[idx]) continue;

            // Beyaz top: yüksek parlaklık, düşük doygunluk (gri değil, parlak beyaz)
            white[idx]  = val > 0.78f && sat < 0.22f;
            // Sarı top: H=22-68 (sarı bant), yüksek doygunluk, orta parlaklık
            yellow[idx] = hue is >= 22f and <= 68f && sat > 0.45f && val > 0.35f;
            // Kırmızı top: H=0-20 veya 340-360 (kırmızı bant), yüksek doygunluk
            red[idx]    = (hue <= 20f || hue >= 340f) && sat > 0.45f && val > 0.25f;
        }

        return (green, white, yellow, red);
    }

    // ── Köşe tespiti ──────────────────────────────────────────────────────────

    /// <summary>
    /// Yeşil bölgenin dört köşelik ekstrem noktalarını bulur.
    ///
    /// Yöntem — köşegen projeksiyon:
    ///   Sol-Üst  : min(x + y)   → sol-üst köşeye en yakın yeşil piksel
    ///   Sağ-Üst  : min(y − x)   → sağ-üst köşeye en yakın yeşil piksel
    ///   Sağ-Alt  : max(x + y)   → sağ-alt köşeye en yakın yeşil piksel
    ///   Sol-Alt  : max(y − x)   → sol-alt köşeye en yakın yeşil piksel
    ///
    /// Bu dört nokta, masanın perspektif bozukluğunu içeren dörtgenini oluşturur.
    /// </summary>
    private static List<(float X, float Y)> FindCorners(bool[] green, int w, int h, float scale)
    {
        float tlV = float.MaxValue, trV = float.MaxValue;
        float brV = float.MinValue, blV = float.MinValue;
        float tlX = 0, tlY = 0, trX = w - 1, trY = 0;
        float brX = w - 1, brY = h - 1, blX = 0, blY = h - 1;
        int count = 0;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!green[y * w + x]) continue;
            count++;
            float s = x + y, d = y - x;
            if (s < tlV) { tlV = s; tlX = x; tlY = y; }
            if (d < trV) { trV = d; trX = x; trY = y; }
            if (s > brV) { brV = s; brX = x; brY = y; }
            if (d > blV) { blV = d; blX = x; blY = y; }
        }

        // Yeşil piksel sayısı toplam alanın %5'inden azsa masa bulunamadı
        if (count < w * h / 20)
            return [];

        float inv = 1f / scale; // küçük kopya koordinatlarını orijinale geri çevir
        return
        [
            (tlX * inv, tlY * inv),
            (trX * inv, trY * inv),
            (brX * inv, brY * inv),
            (blX * inv, blY * inv),
        ];
    }

    // ── Renk bazlı top tespiti ────────────────────────────────────────────────

    private static List<TableBall> DetectByColor(
        bool[] white, bool[] yellow, bool[] red, int w, int h)
    {
        var balls = new List<TableBall>(3);
        TryAddBall(white,  w, h, BallColor.White,  balls);
        TryAddBall(yellow, w, h, BallColor.Yellow, balls);
        TryAddBall(red,    w, h, BallColor.Red,    balls);
        return balls;
    }

    /// <summary>
    /// Verilen renk maskesinde en büyük bağlı bileşeni bulur ve top olarak ekler.
    ///
    /// Bağlı bileşen (connected component): 4-komşuluk ile birbirine bağlı true pikseller.
    /// BFS (Breadth-First Search / Genişlik Öncelikli Arama) flood fill ile bulunur.
    /// En büyük bileşenin merkezini (centroid) ve yarıçapını (alan → daire formülü) hesaplar.
    /// </summary>
    private static void TryAddBall(bool[] mask, int w, int h, BallColor color, List<TableBall> results)
    {
        var visited = new bool[w * h];
        BlobInfo? best = null;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!mask[i] || visited[i]) continue;
            var blob = FloodFill(mask, visited, x, y, w, h);
            if (best is null || blob.Count > best.Count)
                best = blob;
        }

        if (best is null || best.Count < 25) return; // çok az piksel → gürültü

        // Alan = π × r² → r = √(Alan / π)
        float radius = MathF.Sqrt(best.Count / MathF.PI);

        // Boyut kontrol: çok küçük (gürültü) veya çok büyük (top değil) blobleri ele
        float minR = Math.Max(w, h) * 0.008f; // minimum top yarıçapı
        float maxR = Math.Max(w, h) * 0.14f;  // maksimum top yarıçapı
        if (radius < minR || radius > maxR) return;

        float cx = best.SumX / (float)best.Count / w;  // [0,1] normalize merkez X
        float cy = best.SumY / (float)best.Count / h;  // [0,1] normalize merkez Y
        float r  = radius / Math.Max(w, h);             // [0,1] normalize yarıçap

        results.Add(new TableBall(color, cx, cy, r));
    }

    private record BlobInfo(long SumX, long SumY, int Count);

    /// <summary>
    /// BFS flood fill: (sx, sy) başlangıç noktasından başlayarak mask=true olan
    /// tüm komşu pikselleri ziyaret eder ve blobun istatistiklerini döndürür.
    /// </summary>
    private static BlobInfo FloodFill(bool[] mask, bool[] visited, int sx, int sy, int w, int h)
    {
        var queue = new Queue<int>(256);
        int startIdx = sy * w + sx;
        queue.Enqueue(startIdx);
        visited[startIdx] = true;

        long sumX = 0, sumY = 0;
        int count = 0;

        while (queue.Count > 0)
        {
            int idx  = queue.Dequeue();
            int x    = idx % w;
            int y    = idx / w;
            sumX += x; sumY += y; count++;

            // 4-komşuluk: sağ, sol, aşağı, yukarı
            // Satır sınırı: x±1 geçişi yanlış satıra atlamasın diye kontrol edilir
            TryEnqueue(idx + 1, x + 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx - 1, x - 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx + w, x,     y + 1, w, h, mask, visited, queue);
            TryEnqueue(idx - w, x,     y - 1, w, h, mask, visited, queue);
        }

        return new BlobInfo(sumX, sumY, count);
    }

    private static void TryEnqueue(int ni, int nx, int ny, int w, int h,
        bool[] mask, bool[] visited, Queue<int> queue)
    {
        if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
        if (visited[ni] || !mask[ni]) return;
        visited[ni] = true;
        queue.Enqueue(ni);
    }

    // ── Yardımcı hesaplamalar ─────────────────────────────────────────────────

    private static int EstimateTableWidth(bool[] green, int w, int h)
    {
        int minX = w, maxX = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!green[y * w + x]) continue;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
        return Math.Max(1, maxX - minX);
    }

    // ── Görüntü bindirme (annotation) ────────────────────────────────────────

    private static byte[] DrawAnnotations(
        SKBitmap orig, List<TableBall> balls, List<(float X, float Y)> corners)
    {
        using var surface = SKSurface.Create(new SKImageInfo(orig.Width, orig.Height));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(orig, 0, 0);

        float refDim = Math.Max(orig.Width, orig.Height);
        float sw     = Math.Max(3f, refDim / 250f); // çizgi kalınlığı görüntü boyutuna göre

        // ── Masa dörtgeni: kesik sarı çizgi + köşe noktaları ─────────────────
        if (corners.Count == 4)
        {
            // Kesik çizgi efekti: SKPathEffect.CreateDash
            using var linePaint = new SKPaint
            {
                Color       = new SKColor(255, 215, 0, 210), // altın sarısı
                StrokeWidth = sw,
                Style       = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect  = SKPathEffect.CreateDash([sw * 5, sw * 2.5f], 0f)
            };
            using var dotPaint = new SKPaint
            {
                Color       = new SKColor(255, 215, 0, 255),
                Style       = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using var dotBorderPaint = new SKPaint
            {
                Color       = new SKColor(0, 0, 0, 180),
                StrokeWidth = Math.Max(2f, sw * 0.6f),
                Style       = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var path = new SKPath();
            path.MoveTo(corners[0].X, corners[0].Y);
            for (int i = 1; i < corners.Count; i++)
                path.LineTo(corners[i].X, corners[i].Y);
            path.Close();
            canvas.DrawPath(path, linePaint);

            // Köşe noktaları: siyah kenarlıklı sarı daireler
            float dotR = Math.Max(10f, refDim / 100f);
            foreach (var (cx, cy) in corners)
            {
                canvas.DrawCircle(cx, cy, dotR, dotPaint);
                canvas.DrawCircle(cx, cy, dotR, dotBorderPaint);
            }
        }

        // ── Toplar: yarı saydam renkli daire + beyaz etiket ───────────────────
        foreach (var ball in balls)
        {
            float cx = ball.CenterX * orig.Width;
            float cy = ball.CenterY * orig.Height;
            float r  = Math.Max(ball.Radius * refDim, refDim * 0.016f); // en az görünür boyut

            var (fillColor, strokeColor, label) = ball.Color switch
            {
                BallColor.White  => (new SKColor(255, 255, 255, 155), new SKColor(160, 160, 160, 255), "Ak"),
                BallColor.Yellow => (new SKColor(255, 210, 0,   155), new SKColor(170, 130, 0,   255), "Sa"),
                BallColor.Red    => (new SKColor(220, 40,  40,  155), new SKColor(150, 0,   0,   255), "Kı"),
                _                => (SKColors.Gray, SKColors.DarkGray, "?")
            };

            float textSize = Math.Max(16f, r * 0.6f);
            float strokeW  = Math.Max(2.5f, r * 0.12f);

            using var fillP   = new SKPaint { Color = fillColor,   Style = SKPaintStyle.Fill,   IsAntialias = true };
            using var strokeP = new SKPaint { Color = strokeColor, Style = SKPaintStyle.Stroke, StrokeWidth = strokeW, IsAntialias = true };
            // Metin gölge + asıl metin için iki ayrı paint
            using var shadowP = new SKPaint { Color = SKColors.Black, TextSize = textSize, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };
            using var textP   = new SKPaint { Color = SKColors.White, TextSize = textSize, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };

            canvas.DrawCircle(cx, cy, r, fillP);
            canvas.DrawCircle(cx, cy, r, strokeP);
            float textY = cy + textSize * 0.38f;
            canvas.DrawText(label, cx + 1.5f, textY + 1.5f, shadowP); // gölge
            canvas.DrawText(label, cx,        textY,        textP);   // asıl
        }

        using var img  = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, 88);
        return data.ToArray();
    }

    // ── Yardımcı metodlar ─────────────────────────────────────────────────────

    /// <summary>Bitmap'i (w × h) boyutuna ölçekler, yeni bir SKBitmap döndürür.</summary>
    private static SKBitmap ScaleBitmap(SKBitmap src, int w, int h)
    {
        var dst = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var c = new SKCanvas(dst);
        c.DrawBitmap(src, new SKRect(0, 0, w, h));
        return dst;
    }

    /// <summary>
    /// RGB rengi HSV'ye çevirir.
    ///   H (Hue / Ton)        : 0–360° — renk türü (kırmızı=0, yeşil=120, mavi=240)
    ///   S (Saturation / Doygunluk): 0–1 — renk yoğunluğu (0=gri, 1=tam renkli)
    ///   V (Value / Parlaklık): 0–1 — parlaklık (0=siyah, 1=en parlak)
    ///
    /// HSV, RGB'ye kıyasla renk tanıma için çok daha uygundur çünkü aydınlatma
    /// değişimleri yalnızca V kanalını etkiler; H ve S görece sabit kalır.
    /// </summary>
    private static void RgbToHsv(byte r, byte g, byte b, out float h, out float s, out float v)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float max = MathF.Max(rf, MathF.Max(gf, bf));
        float min = MathF.Min(rf, MathF.Min(gf, bf));
        float d   = max - min;

        v = max;
        s = max > 1e-5f ? d / max : 0f;

        if (d < 1e-5f) { h = 0f; return; }

        // Hangi kanal en büyük → farklı açı hesabı
        if (max == rf)       h = 60f * (((gf - bf) / d) % 6f);
        else if (max == gf)  h = 60f * ((bf - rf) / d + 2f);
        else                 h = 60f * ((rf - gf) / d + 4f);

        if (h < 0f) h += 360f;
    }

    private static string BuildStatus(List<TableBall> balls, List<(float X, float Y)> corners)
    {
        var parts = new List<string>(4);
        if (balls.Any(b => b.Color == BallColor.White))  parts.Add("beyaz top ✓");
        if (balls.Any(b => b.Color == BallColor.Yellow)) parts.Add("sarı top ✓");
        if (balls.Any(b => b.Color == BallColor.Red))    parts.Add("kırmızı top ✓");
        if (corners.Count == 4)                          parts.Add("4 köşe ✓");

        return parts.Count > 0
            ? string.Join("  ·  ", parts)
            : "Top veya masa algılanamadı — daha iyi aydınlatma ve açıyla tekrar deneyin.";
    }
}
