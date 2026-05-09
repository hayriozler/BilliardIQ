using SkiaSharp;

namespace BilliardIQ.Mobile.Services;

public record TableBall(BallColor Color, float CenterX, float CenterY, float Radius);

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
/// Renk önceliği (önemli — eski sürümde hata kaynağıydı):
///   Beyaz → Sarı → Kırmızı → Yeşil (masa)
///   Sarı topun Hue değeri (≈55-72°) yeşil alt sınırıyla çakışabilir.
///   Bu yüzden top renkleri yeşilden ÖNCE kontrol edilip "continue" ile geçilir;
///   yeşil maske yalnızca top olmayan piksellere uygulanır.
///
/// Köşe tespiti:
///   Görüntü dört eşit bölgeye (quadrant) ayrılır.
///   Her bölgede kendi köşesine en yakın yeşil piksel köşegen projeksiyonla bulunur:
///     Sol-Üst quadrant  → min(x + y)
///     Sağ-Üst quadrant  → min(y − x)
///     Sağ-Alt quadrant  → max(x + y)
///     Sol-Alt quadrant  → max(y − x)
///   Quadrant ayrımı, karşı köşedeki gürültü piksellerinin yanlış köşe
///   noktası seçmesini önler.
/// </summary>
public class TableVisionService(BallDetectionService onnxDetector)
{
    private const int WorkDim = 640;

    public async Task<TableAnalysisResult> AnalyzeAsync(string imagePath)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath);
        return await AnalyzeBytesAsync(bytes);
    }

    public async Task<TableAnalysisResult> AnalyzeBytesAsync(byte[] imageBytes)
    {
        var onnxBalls = await onnxDetector.DetectAsync(imageBytes);
        return await Task.Run(() => RunAnalysis(imageBytes, onnxBalls));
    }

    private static TableAnalysisResult RunAnalysis(byte[] imageBytes, IReadOnlyList<DetectedBall> onnxBalls)
    {
        using var orig = SKBitmap.Decode(imageBytes);
        if (orig is null)
            return new TableAnalysisResult { StatusMessage = "Görüntü okunamadı." };

        int ow = orig.Width, oh = orig.Height;
        float scale = MathF.Min((float)WorkDim / MathF.Max(ow, oh), 1f);
        int ww = MathF.Max(1, (int)(ow * scale));
        int wh = MathF.Max(1, (int)(oh * scale));

        using var work = ScaleBitmap(orig, ww, wh);
        var (greenMask, whiteMask, yellowMask, redMask) = BuildMasks(work, ww, wh);

        var corners = FindCorners(greenMask, ww, wh, scale);

        List<TableBall> balls;
        if (onnxBalls.Count > 0)
        {
            int tableW = EstimateTableWidth(greenMask, ww, wh);
            balls = [..onnxBalls.Select(b => new TableBall(
                b.Color, b.CenterX, b.CenterY,
                tableW * 0.021f / MathF.Max(ww, wh)))];
        }
        else
        {
            balls = DetectByColor(whiteMask, yellowMask, redMask, ww, wh);
        }

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

    /// <summary>
    /// HSV renk maskelerini oluşturur.
    ///
    /// KRİTİK SIRALAMA: Top renkleri yeşilden önce kontrol edilir.
    /// Sarı topun H ≈ 55-72° ile yeşil maskenin alt sınırı (H ≥ 65°) örtüşebilir.
    /// Eğer yeşil önce kontrol edilirse sarı top "masa" olarak sınıflandırılır
    /// ve sarı maske hiç dolmaz → top bulunamaz.
    ///
    /// Çözüm: beyaz → sarı → kırmızı → (kalan her şey için) yeşil
    /// </summary>
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

            // 1. Beyaz top: yüksek parlaklık + düşük doygunluk → top renk kontrolünden ilk geçer
            if (val > 0.78f && sat < 0.22f)
            {
                white[idx] = true;
                continue; // bu piksel başka maskeye girmesin
            }

            // 2. Sarı top: H = 15-78° (geniş sarı-turuncu bant), doygun, orta parlak
            //    Üst sınır 78°'ye çıkarıldı çünkü bazı sarı toplar hafif yeşilimsi görünür.
            //    Yeşilden ÖNCE kontrol edilir; aksi halde H=68-72° olan top yeşil maskesine girer.
            if (hue is >= 15f and <= 78f && sat > 0.38f && val > 0.28f)
            {
                yellow[idx] = true;
                continue;
            }

            // 3. Kırmızı top: H = 0-22° veya 338-360° (kırmızı çemberi), doygun
            if ((hue <= 22f || hue >= 338f) && sat > 0.38f && val > 0.20f)
            {
                red[idx] = true;
                continue;
            }

            // 4. Yeşil (masa örtüsü): yalnızca top olmayan pikseller buraya gelir.
            //    H = 60-200°: geniş aralık; loş ışıkta veya yıpranmış keçede H kayması olabilir.
            //    Saturation eşiği düşük: flaş altında soluk yeşil yüzeyler de yakalanır.
            if (hue is >= 60f and <= 200f && sat > 0.15f && val > 0.08f)
            {
                green[idx] = true;
            }
        }

        return (green, white, yellow, red);
    }

    // ── Köşe tespiti ──────────────────────────────────────────────────────────

    /// <summary>
    /// Görüntüyü dört quadrant'a böler; her quadrant'ta kendi köşesine en yakın
    /// yeşil pikseli köşegen projeksiyon yöntemiyle bulur.
    ///
    /// Quadrant ayrımı neden önemli?
    ///   Eski tek-geçiş yönteminde, sağ-alt köşedeki birkaç gürültü pikseli
    ///   sol-üst köşenin "minimum" değerini ezebiliyordu.
    ///   Quadrant ayrımıyla her köşe kendi bölgesinde aranır → gürültüden etkilenmez.
    ///
    /// Her quadrant için minimum arama yeterli yeşil piksel içermiyorsa
    /// (< 50 piksel) o köşe null döner ve tüm köşe listesi boş döndürülür.
    /// </summary>
    private static List<(float X, float Y)> FindCorners(bool[] green, int w, int h, float scale)
    {
        int hw = w / 2, hh = h / 2;

        // Her quadrant'ta ilgili diagonal ekstrem noktayı bul
        (int X, int Y)? tl = QuadrantExtreme(green, w, 0,  hw, 0,  hh, minimize: true,  diagonal: +1); // min(x+y)
        (int X, int Y)? tr = QuadrantExtreme(green, w, hw, w,  0,  hh, minimize: true,  diagonal: -1); // min(y-x)
        (int X, int Y)? br = QuadrantExtreme(green, w, hw, w,  hh, h,  minimize: false, diagonal: +1); // max(x+y)
        (int X, int Y)? bl = QuadrantExtreme(green, w, 0,  hw, hh, h,  minimize: false, diagonal: -1); // max(y-x)

        if (tl is null || tr is null || br is null || bl is null)
            return [];

        float inv = 1f / scale;
        return
        [
            (tl.Value.X * inv, tl.Value.Y * inv),
            (tr.Value.X * inv, tr.Value.Y * inv),
            (br.Value.X * inv, br.Value.Y * inv),
            (bl.Value.X * inv, bl.Value.Y * inv),
        ];
    }

    /// <summary>
    /// Belirli bir dikdörtgen bölge içindeki yeşil pikseller arasında
    /// diagonal projeksiyon değerine göre en uç noktayı bulur.
    ///   diagonal = +1 → x+y kullanılır (ana köşegen)
    ///   diagonal = -1 → y-x kullanılır (ters köşegen)
    ///   minimize = true  → en küçük değer (sol-üst yönler)
    ///   minimize = false → en büyük değer (sağ-alt yönler)
    /// Bölgede 50'den az yeşil piksel varsa null döner.
    /// </summary>
    private static (int X, int Y)? QuadrantExtreme(
        bool[] green, int w,
        int x0, int x1, int y0, int y1,
        bool minimize, int diagonal)
    {
        int best = minimize ? int.MaxValue : int.MinValue;
        int bx = -1, by = -1, count = 0;

        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            if (!green[y * w + x]) continue;
            count++;
            int proj = diagonal == +1 ? x + y : y - x;
            bool isBetter = minimize ? proj < best : proj > best;
            if (isBetter) { best = proj; bx = x; by = y; }
        }

        return count >= 50 ? (bx, by) : null;
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

        if (best is null || best.Count < 20) return;

        float radius = MathF.Sqrt(best.Count / MathF.PI);
        float dim    = MathF.Max(w, h);
        if (radius < dim * 0.007f || radius > dim * 0.16f) return;

        results.Add(new TableBall(
            color,
            best.SumX / (float)best.Count / w,
            best.SumY / (float)best.Count / h,
            radius / dim));
    }

    private record BlobInfo(long SumX, long SumY, int Count);

    private static BlobInfo FloodFill(bool[] mask, bool[] visited, int sx, int sy, int w, int h)
    {
        var queue = new Queue<int>(256);
        int start = sy * w + sx;
        queue.Enqueue(start);
        visited[start] = true;

        long sumX = 0, sumY = 0;
        int count = 0;

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            int x = idx % w, y = idx / w;
            sumX += x; sumY += y; count++;

            TryEnqueue(idx + 1, x + 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx - 1, x - 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx + w, x, y + 1, w, h, mask, visited, queue);
            TryEnqueue(idx - w, x, y - 1, w, h, mask, visited, queue);
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

    // ── Görüntü bindirme ──────────────────────────────────────────────────────

    private static byte[] DrawAnnotations(
        SKBitmap orig, List<TableBall> balls, List<(float X, float Y)> corners)
    {
        using var surface = SKSurface.Create(new SKImageInfo(orig.Width, orig.Height));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(orig, 0, 0);

        float refDim = MathF.Max(orig.Width, orig.Height);
        float sw     = MathF.Max(3f, refDim / 220f);

        // ── Masa köşeleri ─────────────────────────────────────────────────────
        if (corners.Count == 4)
        {
            // Kenar çizgileri: yarı saydam sarı, kesik
            using var edgePaint = new SKPaint
            {
                Color       = new SKColor(255, 215, 0, 180),
                StrokeWidth = sw,
                Style       = SKPaintStyle.Stroke,
                IsAntialias = true,
                PathEffect  = SKPathEffect.CreateDash([sw * 5, sw * 2.5f], 0f)
            };
            var path = new SKPath();
            path.MoveTo(corners[0].X, corners[0].Y);
            for (int i = 1; i < 4; i++) path.LineTo(corners[i].X, corners[i].Y);
            path.Close();
            canvas.DrawPath(path, edgePaint);

            // Köşe noktaları: L-şekilli belirteç + dolgu daire + etiket
            string[] labels = ["SÜ", "SÜ", "SA", "SA"]; // Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt
            string[] fullLabels = ["Sol-Üst", "Sağ-Üst", "Sağ-Alt", "Sol-Alt"];
            float dotR    = MathF.Max(12f, refDim / 80f);
            float armLen  = dotR * 2.2f;
            float textSz  = MathF.Max(18f, refDim / 55f);

            using var fillP   = new SKPaint { Color = new SKColor(255, 215, 0, 230), Style = SKPaintStyle.Fill,   IsAntialias = true };
            using var borderP = new SKPaint { Color = new SKColor(0, 0, 0, 200),     Style = SKPaintStyle.Stroke, StrokeWidth = MathF.Max(2f, sw * 0.5f), IsAntialias = true };
            using var armP    = new SKPaint { Color = new SKColor(255, 215, 0, 255), Style = SKPaintStyle.Stroke, StrokeWidth = sw * 1.2f, IsAntialias = true, StrokeCap = SKStrokeCap.Round };
            using var textP   = new SKPaint { Color = SKColors.Black, TextSize = textSz, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };
            using var shadowP = new SKPaint { Color = new SKColor(255, 215, 0, 255), TextSize = textSz, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };

            // Köşe indeksine göre L-kollarının yönleri:
            // 0=SolÜst → sağa(+x) ve aşağı(+y), 1=SağÜst → sola(-x) ve aşağı(+y)
            // 2=SağAlt → sola(-x) ve yukarı(-y), 3=SolAlt → sağa(+x) ve yukarı(-y)
            (float dx, float dy)[] armDirs = [(+1, +1), (-1, +1), (-1, -1), (+1, -1)];

            for (int i = 0; i < 4; i++)
            {
                float cx = corners[i].X, cy = corners[i].Y;
                var (adx, ady) = armDirs[i];

                // L kolları
                canvas.DrawLine(cx, cy, cx + adx * armLen, cy, armP);
                canvas.DrawLine(cx, cy, cx, cy + ady * armLen, armP);

                // Dolgu daire
                canvas.DrawCircle(cx, cy, dotR, fillP);
                canvas.DrawCircle(cx, cy, dotR, borderP);

                // Etiket: sarı gölge arkada, siyah öne
                float ty = cy + ady * (dotR + textSz * 1.2f);
                canvas.DrawText(fullLabels[i], cx, ty,     shadowP);
                canvas.DrawText(fullLabels[i], cx, ty + 2, textP);
            }
        }

        // ── Toplar ───────────────────────────────────────────────────────────
        foreach (var ball in balls)
        {
            float cx = ball.CenterX * orig.Width;
            float cy = ball.CenterY * orig.Height;
            float r  = MathF.Max(ball.Radius * refDim, refDim * 0.015f);

            var (fill, stroke, label) = ball.Color switch
            {
                BallColor.White  => (new SKColor(255, 255, 255, 160), new SKColor(160, 160, 160, 255), "Ak"),
                BallColor.Yellow => (new SKColor(255, 210, 0,   160), new SKColor(170, 130, 0,   255), "Sa"),
                BallColor.Red    => (new SKColor(220, 40,  40,  160), new SKColor(150, 0,   0,   255), "Kı"),
                _                => (SKColors.Gray, SKColors.DarkGray, "?")
            };

            float textSz  = MathF.Max(16f, r * 0.55f);
            float strokeW = MathF.Max(2.5f, r * 0.12f);

            using var fillP   = new SKPaint { Color = fill,         Style = SKPaintStyle.Fill,   IsAntialias = true };
            using var strokeP = new SKPaint { Color = stroke,       Style = SKPaintStyle.Stroke, StrokeWidth = strokeW, IsAntialias = true };
            using var textP   = new SKPaint { Color = SKColors.White, TextSize = textSz, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };
            using var shadowP = new SKPaint { Color = SKColors.Black, TextSize = textSz, IsAntialias = true, FakeBoldText = true, TextAlign = SKTextAlign.Center };

            canvas.DrawCircle(cx, cy, r, fillP);
            canvas.DrawCircle(cx, cy, r, strokeP);
            float textY = cy + textSz * 0.38f;
            canvas.DrawText(label, cx + 1.5f, textY + 1.5f, shadowP);
            canvas.DrawText(label, cx,        textY,        textP);
        }

        using var img  = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, 88);
        return data.ToArray();
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    private static SKBitmap ScaleBitmap(SKBitmap src, int w, int h)
    {
        var dst = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var c = new SKCanvas(dst);
        c.DrawBitmap(src, new SKRect(0, 0, w, h));
        return dst;
    }

    /// <summary>
    /// RGB → HSV dönüşümü.
    ///   H (Hue / Ton)              : 0–360°
    ///   S (Saturation / Doygunluk) : 0–1
    ///   V (Value / Parlaklık)      : 0–1
    ///
    /// HSV, renk sınıflandırması için RGB'den üstündür: aydınlatma değişimleri
    /// yalnızca V'yi etkilerken H ve S büyük ölçüde sabit kalır.
    /// Bu sayede farklı ışık koşullarında aynı HSV eşikleri çalışır.
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

        if (max == rf)      h = 60f * (((gf - bf) / d) % 6f);
        else if (max == gf) h = 60f * ((bf - rf) / d + 2f);
        else                h = 60f * ((rf - gf) / d + 4f);

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
            : "Hiçbir şey algılanamadı — daha iyi aydınlatma ve dik açıyla tekrar deneyin.";
    }
}
