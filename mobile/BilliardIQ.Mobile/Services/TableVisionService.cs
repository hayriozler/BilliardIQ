using SkiaSharp;

namespace BilliardIQ.Mobile.Services;

// Kısayol: LocalizationManager.Instance["key"]
file static class L
{
    public static string Get(string key) => LocalizationManager.Instance[key];
}

public enum DetectionEngine
{
    /// <summary>HSV threshold: largest color blob per color. Fast, may pick table frames over balls.</summary>
    Color,
    /// <summary>OpenCV (EmguCV): HoughCircles for balls, findContours+ApproxPolyDP for corners.</summary>
    OpenCv,
    /// <summary>YOLOv8 ONNX model. Falls back to Color if model file is missing.</summary>
    Onnx
}

public record TableBall(BallColor Color, float CenterX, float CenterY, float Radius,
    int PixelX = 0, int PixelY = 0, int PixelRadius = 0);

public class TableAnalysisResult
{
    public IReadOnlyList<TableBall> Balls { get; init; } = [];
    /// <summary>Dört köşe: Sol-Üst, Sağ-Üst, Sağ-Alt, Sol-Alt — piksel koordinatları.</summary>
    public IReadOnlyList<(float X, float Y)> Corners { get; init; } = [];
    public byte[] AnnotatedImage { get; init; } = [];
    public string StatusMessage { get; init; } = string.Empty;
    public DetectionEngine UsedEngine { get; init; }
}

/// <summary>
/// Bilardo masası fotoğrafından top ve masa köşelerini tespit eder.
///
/// Renk önceliği (kritik — eski sürümde hata kaynağıydı):
///   Beyaz → Sarı → Kırmızı → Yeşil
///   Sarı topun H ≈ 55-72° yeşil alt sınırıyla çakışır; yeşil önce kontrol edilirse
///   sarı top "masa" olarak sınıflandırılır ve sarı maske boş kalır.
///
/// Engine.Color  → en büyük renk blobu (basit, hızlı)
/// Engine.Hough  → en dairesel renk blobu (beyaz bant/çerçeve gürültüsünü eler)
///   Dairesellik = piksel_sayısı / (π × sınır_yarıçapı²)
///   Daire için ≈ 1.0, yatay bant için ≈ 0.05 → eşik > 0.40 ile elenir.
///
/// Köşe tespiti (Quadrant Dijagonal Projeksiyon):
///   Görüntü 4 çeyreğe bölünür; her çeyrekte kendi köşesine en yakın yeşil
///   piksel bulunur ve 8-piksel komşuluk ortalamasıyla gürültü azaltılır.
/// </summary>
public class TableVisionService(BallDetectionService onnxDetector, OpenCvBallDetector openCvDetector)
{
    private const int _workDim = 640;
    private const float _minCircularity = 0.35f;

    public async Task<TableAnalysisResult> AnalyzeAsync(string imagePath,
        DetectionEngine engine = DetectionEngine.OpenCv)
    {
        var bytes = await File.ReadAllBytesAsync(imagePath);
        return await AnalyzeBytesAsync(bytes, engine);
    }

    public async Task<TableAnalysisResult> AnalyzeBytesAsync(byte[] imageBytes,
        DetectionEngine engine = DetectionEngine.OpenCv)
    {
        // OpenCV engine: EmguCV ile ayrı iş parçacığında çalıştır
        if (engine == DetectionEngine.OpenCv)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var (balls, corners) = openCvDetector.Detect(imageBytes);
                    // Annotated görüntüyü SkiaSharp ile çiz (SkiaSharp daha iyi JPEG kalitesi verir)
                    using var orig = SkiaSharp.SKBitmap.Decode(imageBytes);
                    byte[] annotated = orig is not null
                        ? DrawAnnotations(orig, balls, corners)
                        : imageBytes;
                    return new TableAnalysisResult
                    {
                        Balls          = balls,
                        Corners        = corners,
                        AnnotatedImage = annotated,
                        StatusMessage  = BuildStatus(balls, corners, engine),
                        UsedEngine     = engine
                    };
                }
                catch (Exception ex)
                {
                    return new TableAnalysisResult
                    {
                        StatusMessage = $"OpenCV hatası: {ex.Message}",
                        UsedEngine    = engine
                    };
                }
            });
        }

        // ONNX: try model, fall back to Color algorithm if model is missing
        IReadOnlyList<DetectedBall> onnxBalls = [];
        DetectionEngine algorithmEngine = engine; // may change on fallback

        if (engine == DetectionEngine.Onnx)
        {
            onnxBalls = await onnxDetector.DetectAsync(imageBytes);
            if (onnxBalls.Count == 0) algorithmEngine = DetectionEngine.Color;
        }

        // Pass original requested engine separately so status always reflects what the user selected
        return await Task.Run(() => RunAnalysis(imageBytes, onnxBalls, algorithmEngine, engine));
    }

    private static TableAnalysisResult RunAnalysis(
        byte[] imageBytes, IReadOnlyList<DetectedBall> onnxBalls,
        DetectionEngine engine, DetectionEngine requestedEngine)
    {
        using var orig = SKBitmap.Decode(imageBytes);
        if (orig is null)
            return new TableAnalysisResult { StatusMessage = L.Get("TV_ReadError") };

        int ow = orig.Width, oh = orig.Height;
        float scale = MathF.Min((float)_workDim / MathF.Max(ow, oh), 1f);
        int ww = Math.Max(1, (int)(ow * scale));
        int wh = Math.Max(1, (int)(oh * scale));

        using var work = ScaleBitmap(orig, ww, wh);
        var (greenMask, whiteMask, yellowMask, redMask) = BuildMasks(work, ww, wh);

        // Köşe tespiti: yeşil maskeden quadrant projeksiyonu
        var corners = FindCorners(greenMask, ww, wh, scale);

        List<TableBall> balls;
        if (onnxBalls.Count > 0)
        {
            // ONNX: relative [0,1] koordinatları, piksel de hesaplanır
            int tblW = EstimateTableWidth(greenMask, ww, wh);
            float rRel = tblW * 0.021f / MathF.Max(ww, wh);
            balls = [..onnxBalls.Select(b => new TableBall(
                b.Color,
                b.CenterX, b.CenterY, rRel,
                (int)(b.CenterX * ow), (int)(b.CenterY * oh),
                (int)(rRel * MathF.Max(ow, oh))))];
        }
        else
        {
            // Color: largest blob per color (fastest, may pick table borders over balls)
            // OpenCV handles circularity properly via HoughCircles — no overlap here
            balls = DetectByColor(whiteMask, yellowMask, redMask, ww, wh, ow, oh,
                useCircularity: false);
        }

        byte[] annotated = DrawAnnotations(orig, balls, corners);

        // Status shows the engine the user requested (e.g. [Onnx] even when it fell back to Color)
        string fallbackNote = (requestedEngine == DetectionEngine.Onnx && engine == DetectionEngine.Color)
            ? " (no model→Color)" : string.Empty;

        return new TableAnalysisResult
        {
            Balls          = balls,
            Corners        = corners,
            AnnotatedImage = annotated,
            StatusMessage  = BuildStatus(balls, corners, requestedEngine) + fallbackNote,
            UsedEngine     = requestedEngine
        };
    }

    // ── Renk maskeleri ────────────────────────────────────────────────────────

    /// <summary>
    /// HSV renk maskelerini oluşturur. Sıralama kritik:
    ///   1. Beyaz (val yüksek, sat düşük)
    ///   2. Sarı  (H=15-78°, yüksek doygunluk)  ← yeşilden ÖNCE — H örtüşmesi önlenir
    ///   3. Kırmızı (H=0-22° veya 338-360°)
    ///   4. Yeşil (geri kalan — masa örtüsü)
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

            if (val > 0.75f && sat < 0.25f)  { white[idx]  = true; continue; }
            if (hue is >= 15f and <= 78f && sat > 0.35f && val > 0.25f) { yellow[idx] = true; continue; }
            if ((hue <= 22f || hue >= 338f) && sat > 0.35f && val > 0.18f) { red[idx] = true; continue; }
            if (hue is >= 55f and <= 200f && sat > 0.13f && val > 0.07f) green[idx] = true;
        }

        return (green, white, yellow, red);
    }

    // ── Köşe tespiti ──────────────────────────────────────────────────────────

    /// <summary>
    /// Görüntüyü 4 quadrant'a böler. Her quadrant'ta yeşil pikseller arasından
    /// köşegen projeksiyon ile köşeye en yakın noktayı bulur; sonra o noktanın
    /// 8-piksel komşuluğundaki yeşil piksellerin ortalaması alınır (gürültü azaltma).
    ///
    /// Quadrant başına 30'dan az yeşil piksel varsa → null → köşeler döndürülmez.
    /// </summary>
    private static List<(float X, float Y)> FindCorners(bool[] green, int w, int h, float scale)
    {
        int hw = w / 2, hh = h / 2;

        var tl = QuadrantExtreme(green, w, 0,  hw, 0,  hh, minimize: true,  diag: +1);
        var tr = QuadrantExtreme(green, w, hw, w,  0,  hh, minimize: true,  diag: -1);
        var br = QuadrantExtreme(green, w, hw, w,  hh, h,  minimize: false, diag: +1);
        var bl = QuadrantExtreme(green, w, 0,  hw, hh, h,  minimize: false, diag: -1);

        if (tl is null || tr is null || br is null || bl is null) return [];

        float inv = 1f / scale;
        return
        [
            (tl.Value.X * inv, tl.Value.Y * inv),
            (tr.Value.X * inv, tr.Value.Y * inv),
            (br.Value.X * inv, br.Value.Y * inv),
            (bl.Value.X * inv, bl.Value.Y * inv),
        ];
    }

    private static (float X, float Y)? QuadrantExtreme(
        bool[] green, int w,
        int x0, int x1, int y0, int y1,
        bool minimize, int diag)
    {
        int best = minimize ? int.MaxValue : int.MinValue;
        int bx = -1, by = -1, count = 0;

        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            if (!green[y * w + x]) continue;
            count++;
            int proj = diag == +1 ? x + y : y - x;
            if (minimize ? proj < best : proj > best) { best = proj; bx = x; by = y; }
        }

        if (count < 30 || bx < 0) return null;

        // En uç noktanın 8-piksel komşuluğundaki yeşil piksellerin ortalaması → gürültü azalır
        const int R = 8;
        float sx = 0, sy = 0;
        int n = 0;
        for (int y = Math.Max(y0, by - R); y <= Math.Min(y1 - 1, by + R); y++)
        for (int x = Math.Max(x0, bx - R); x <= Math.Min(x1 - 1, bx + R); x++)
        {
            if (!green[y * w + x]) continue;
            int proj = diag == +1 ? x + y : y - x;
            // Sadece benzer projeksiyon değerine sahip komşular dahil edilir
            if (minimize ? proj > best + R * 2 : proj < best - R * 2) continue;
            sx += x; sy += y; n++;
        }

        return n > 0 ? (sx / n, sy / n) : (bx, by);
    }

    // ── Top tespiti ───────────────────────────────────────────────────────────

    private static List<TableBall> DetectByColor(
        bool[] white, bool[] yellow, bool[] red,
        int ww, int wh, int origW, int origH,
        bool useCircularity)
    {
        var balls = new List<TableBall>(3);
        TryAddBall(white,  ww, wh, BallColor.White,  origW, origH, useCircularity, balls);
        TryAddBall(yellow, ww, wh, BallColor.Yellow, origW, origH, useCircularity, balls);
        TryAddBall(red,    ww, wh, BallColor.Red,    origW, origH, useCircularity, balls);
        return balls;
    }

    /// <summary>
    /// Renk maskesindeki tüm bağlı bileşenleri (blob) bulur.
    ///
    /// Color engine  → en büyük blobu seçer (eski davranış).
    /// Hough engine → bloblara dairesellik skoru hesaplanır:
    ///   Dairesellik = piksel_sayısı / (π × sınır_yarıçapı²)
    ///   Daire için ≈ 1.0 | Yatay bant (bant/çerçeve) için ≈ 0.05
    ///   En yüksek dairesellikli ve MinCircularity üstündeki blob seçilir.
    ///   Bu sayede beyaz bant/ray → elenır, beyaz bilardo topu → seçilir.
    /// </summary>
    private static void TryAddBall(
        bool[] mask, int w, int h, BallColor color,
        int origW, int origH,
        bool useCircularity, List<TableBall> results)
    {
        var visited = new bool[w * h];
        BlobInfo? chosen = null;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!mask[i] || visited[i]) continue;

            var blob = FloodFill(mask, visited, x, y, w, h);
            if (blob.Count < 15) continue;

            // Bounding-box sınır yarıçapı = bounding kutunun büyük kenarının yarısı
            float boundR = MathF.Max(blob.MaxX - blob.MinX, blob.MaxY - blob.MinY) / 2f;
            if (boundR < 1f) continue;

            float circularity = blob.Count / (MathF.PI * boundR * boundR);

            // Boyut kontrolü: masanın 0.5%-13% aralığında
            float dim = MathF.Max(w, h);
            if (boundR < dim * 0.005f || boundR > dim * 0.13f) continue;

            if (useCircularity)
            {
                // Hough: dairesellik eşiği + en dairesel blob
                if (circularity < _minCircularity) continue;
                if (chosen is null || circularity > chosen.Circularity)
                    chosen = blob with { Circularity = circularity };
            }
            else
            {
                // Color: en büyük blob
                if (chosen is null || blob.Count > chosen.Count)
                    chosen = blob with { Circularity = circularity };
            }
        }

        if (chosen is null) return;

        float cx  = chosen.SumX / (float)chosen.Count / w;
        float cy  = chosen.SumY / (float)chosen.Count / h;
        float r   = (chosen.MaxX - chosen.MinX + chosen.MaxY - chosen.MinY) / 4f / MathF.Max(w, h);

        int px = (int)(cx * origW);
        int py = (int)(cy * origH);
        int pr = (int)(r * MathF.Max(origW, origH));

        results.Add(new TableBall(color, cx, cy, r, px, py, pr));
    }

    private record BlobInfo(long SumX, long SumY, int Count,
        int MinX, int MaxX, int MinY, int MaxY, float Circularity = 0f);

    private static BlobInfo FloodFill(bool[] mask, bool[] visited, int sx, int sy, int w, int h)
    {
        var queue = new Queue<int>(256);
        int start = sy * w + sx;
        queue.Enqueue(start);
        visited[start] = true;

        long sumX = 0, sumY = 0;
        int count = 0, minX = sx, maxX = sx, minY = sy, maxY = sy;

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            int x = idx % w, y = idx / w;
            sumX += x; sumY += y; count++;
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;

            TryEnqueue(idx + 1, x + 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx - 1, x - 1, y, w, h, mask, visited, queue);
            TryEnqueue(idx + w, x, y + 1, w, h, mask, visited, queue);
            TryEnqueue(idx - w, x, y - 1, w, h, mask, visited, queue);
        }

        return new BlobInfo(sumX, sumY, count, minX, maxX, minY, maxY);
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
            using var edgePaint = new SKPaint
            {
                Color       = new SKColor(255, 215, 0, 200),
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

            using var dotFill   = new SKPaint { Color = new SKColor(255, 215, 0, 240), Style = SKPaintStyle.Fill,   IsAntialias = true };
            using var dotBorder = new SKPaint { Color = SKColors.Black,                Style = SKPaintStyle.Stroke, StrokeWidth = sw * 0.5f, IsAntialias = true };
            using var armPaint  = new SKPaint { Color = new SKColor(255, 215, 0, 255), Style = SKPaintStyle.Stroke, StrokeWidth = sw * 1.1f, IsAntialias = true, StrokeCap = SKStrokeCap.Round };
            using var lblWhite  = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var lblBlack  = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var lblFont   = new SKFont { Size = MathF.Max(16f, refDim / 60f), Embolden = true };

            float dotR   = MathF.Max(12f, refDim / 85f);
            float armLen = dotR * 2.2f;
            (float dx, float dy)[] dirs = [(+1, +1), (-1, +1), (-1, -1), (+1, -1)];
            string[] fullLbls =
            [
                L.Get("TV_TopLeft"), L.Get("TV_TopRight"),
                L.Get("TV_BottomRight"), L.Get("TV_BottomLeft")
            ];

            for (int i = 0; i < 4; i++)
            {
                float cx = corners[i].X, cy = corners[i].Y;
                var (adx, ady) = dirs[i];
                canvas.DrawLine(cx, cy, cx + adx * armLen, cy, armPaint);
                canvas.DrawLine(cx, cy, cx, cy + ady * armLen, armPaint);
                canvas.DrawCircle(cx, cy, dotR, dotFill);
                canvas.DrawCircle(cx, cy, dotR, dotBorder);
                float ty = cy + ady * (dotR + lblFont.Size * 1.3f);
                canvas.DrawText(fullLbls[i], cx, ty,      SKTextAlign.Center, lblFont, lblBlack);
                canvas.DrawText(fullLbls[i], cx, ty - 1f, SKTextAlign.Center, lblFont, lblWhite);
            }
        }

        // ── Toplar ───────────────────────────────────────────────────────────
        foreach (var ball in balls)
        {
            float cx = ball.CenterX * orig.Width;
            float cy = ball.CenterY * orig.Height;
            float r  = MathF.Max(ball.Radius * refDim, refDim * 0.014f);

            var (fill, stroke, label) = ball.Color switch
            {
                BallColor.White  => (new SKColor(255, 255, 255, 155), new SKColor(140, 140, 140, 255), "Ak"),
                BallColor.Yellow => (new SKColor(255, 210, 0,   155), new SKColor(160, 120, 0,   255), "Sa"),
                BallColor.Red    => (new SKColor(220, 40,  40,  155), new SKColor(140, 0,   0,   255), "Kı"),
                _                => (SKColors.Gray, SKColors.DarkGray, "?")
            };

            float ts = MathF.Max(16f, r * 0.55f);
            float sw2 = MathF.Max(2.5f, r * 0.12f);

            using var fillP   = new SKPaint { Color = fill,          Style = SKPaintStyle.Fill,   IsAntialias = true };
            using var strokeP = new SKPaint { Color = stroke,        Style = SKPaintStyle.Stroke, StrokeWidth = sw2, IsAntialias = true };
            using var textW   = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var textB   = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            using var textFont = new SKFont { Size = ts, Embolden = true };

            canvas.DrawCircle(cx, cy, r, fillP);
            canvas.DrawCircle(cx, cy, r, strokeP);
            float ty = cy + ts * 0.38f;
            canvas.DrawText(label, cx + 1.5f, ty + 1.5f, SKTextAlign.Center, textFont, textB);
            canvas.DrawText(label, cx,        ty,        SKTextAlign.Center, textFont, textW);
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
    /// RGB → HSV. H: 0-360°, S: 0-1, V: 0-1.
    /// HSV, aydınlatma değişimlerinden etkilenmez; H ve S büyük ölçüde sabit kalır.
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

    private static string BuildStatus(List<TableBall> balls,
        List<(float X, float Y)> corners, DetectionEngine engine)
    {
        var parts = new List<string>(5) { $"[{engine}]" };
        if (balls.Any(b => b.Color == BallColor.White))  parts.Add(L.Get("TV_White")  + " ✓");
        if (balls.Any(b => b.Color == BallColor.Yellow)) parts.Add(L.Get("TV_Yellow") + " ✓");
        if (balls.Any(b => b.Color == BallColor.Red))    parts.Add(L.Get("TV_Red")    + " ✓");
        if (corners.Count == 4)                          parts.Add(L.Get("TV_Corners") + " ✓");

        return parts.Count > 1
            ? string.Join("  ·  ", parts)
            : L.Get("TV_NotFound");
    }
}
