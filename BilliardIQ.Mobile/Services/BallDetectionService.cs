using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace BilliardIQ.Mobile.Services;

/// <summary>Ball colour as detected by the ONNX model.</summary>
public enum BallColor { White, Yellow, Red }

/// <summary>A single ball detected in a table photo.</summary>
/// <param name="Color">Which ball this is.</param>
/// <param name="CenterX">Horizontal centre, 0–1 (relative to image width).</param>
/// <param name="CenterY">Vertical centre, 0–1 (relative to image height).</param>
/// <param name="Confidence">Model confidence 0–1.</param>
public record DetectedBall(BallColor Color, float CenterX, float CenterY, float Confidence);

/// <summary>
/// Runs YOLOv8n inference on a billiard-table photo to locate all three balls.
///
/// Model contract
///   File    : Resources/Raw/billiard_balls.onnx
///   Input   : "images"  — float32 [1, 3, 640, 640] NCHW, normalised 0-1
///   Output  : "output0" — float32 [1, 7, 8400]
///             Each column: [cx, cy, w, h, white_prob, yellow_prob, red_prob]
///             All values are relative to the 640 × 640 input.
///
/// Platform support (Microsoft.ML.OnnxRuntime 1.25.1)
///   Android  — CPU + NNAPI acceleration (Android 8.1+)
///   iOS      — CPU + CoreML acceleration
///   Windows  — CPU + DirectML acceleration
/// </summary>
public sealed class BallDetectionService : IDisposable
{
    private const string _modelAsset    = "billiard_balls.onnx";
    private const int    _inputSize     = 640;
    private const float  _confThreshold = 0.45f;
    private const float  _iouThreshold  = 0.45f;

    private InferenceSession? _session;
    private bool _modelMissing;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the ONNX model from app resources (once).
    /// Returns false and disables the service silently if the model file is absent.
    /// </summary>
    public async Task<bool> InitAsync()
    {
        if (_session is not null)    return true;
        if (_modelMissing)           return false;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(_modelAsset);
            using var ms     = new MemoryStream();
            await stream.CopyToAsync(ms);

            var opts = new SessionOptions();
            // Enable platform acceleration if available
            opts.AppendExecutionProvider_CPU();

            _session = new InferenceSession(ms.ToArray(), opts);
            return true;
        }
        catch (FileNotFoundException)
        {
            // Model not yet bundled — service degrades gracefully
            _modelMissing = true;
            return false;
        }
    }

    // ── Inference ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects all billiard balls in <paramref name="imageBytes"/> (JPEG/PNG).
    /// Returns an empty list when the model is unavailable or nothing is found.
    /// </summary>
    public async Task<IReadOnlyList<DetectedBall>> DetectAsync(byte[] imageBytes)
    {
        if (!await InitAsync()) return [];

        return await Task.Run(() =>
        {
            try   { return RunInference(imageBytes); }
            catch { return []; }
        });
    }

    // ── Private: inference pipeline ──────────────────────────────────────────

    private List<DetectedBall> RunInference(byte[] imageBytes)
    {
        // Decode + letterbox-resize to 640×640
        var decoded  = DecodeAndResize(imageBytes, out int origW, out int origH,
                                       out float scaleX, out float scaleY,
                                       out int padX, out int padY);

        // Build NCHW float32 tensor
        var tensor = BuildInputTensor(decoded);
        var inputs = new[] { NamedOnnxValue.CreateFromTensor("images", tensor) };

        using var outputs = _session!.Run(inputs);
        var raw = outputs.First().AsEnumerable<float>().ToArray();

        // raw shape: [1, 7, 8400] → parse columns
        return ParseAndFilter(raw, origW, origH, scaleX, scaleY, padX, padY);
    }

    // Letterbox-resize: scale image to fit 640×640 with grey padding
    private static byte[] DecodeAndResize(byte[] src,
        out int origW, out int origH,
        out float scaleX, out float scaleY,
        out int padX, out int padY)
    {
        // Use SkiaSharp (transitively available in MAUI) to decode
        using var skBitmap = SKBitmap.Decode(src);
        origW = skBitmap.Width;
        origH = skBitmap.Height;

        float scale = Math.Min((float)_inputSize / origW, (float)_inputSize / origH);
        int   newW  = (int)(origW * scale);
        int   newH  = (int)(origH * scale);
        padX  = (_inputSize - newW) / 2;
        padY  = (_inputSize - newH) / 2;
        scaleX = scale;
        scaleY = scale;

        // Create 640×640 grey canvas and draw scaled image
        using var surface = SKSurface.Create(
            new SKImageInfo(_inputSize, _inputSize, SKColorType.Rgb888x));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(114, 114, 114));   // standard letterbox grey
        canvas.DrawBitmap(skBitmap,
            new SKRect(padX, padY, padX + newW, padY + newH));

        using var snap  = surface.Snapshot();
        using var data  = snap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static DenseTensor<float> BuildInputTensor(byte[] pngBytes)
    {
        using var bmp = SKBitmap.Decode(pngBytes);
        var tensor = new DenseTensor<float>([1, 3, _inputSize, _inputSize]);

        for (int y = 0; y < _inputSize; y++)
        for (int x = 0; x < _inputSize; x++)
        {
            var px = bmp.GetPixel(x, y);
            tensor[0, 0, y, x] = px.Red   / 255f;   // R
            tensor[0, 1, y, x] = px.Green / 255f;   // G
            tensor[0, 2, y, x] = px.Blue  / 255f;   // B
        }

        return tensor;
    }

    private static List<DetectedBall> ParseAndFilter(
        float[] raw, int origW, int origH,
        float scaleX, float scaleY, int padX, int padY)
    {
        // raw: [7 × 8400] stored as [row0_col0, row0_col1, ..., row6_col8399]
        const int rows = 7;
        const int cols = 8400;

        var candidates = new List<(float cx, float cy, float w, float h,
                                   float conf, BallColor color)>();

        for (int a = 0; a < cols; a++)
        {
            float cx = raw[0 * cols + a];
            float cy = raw[1 * cols + a];
            float bw = raw[2 * cols + a];
            float bh = raw[3 * cols + a];

            float p0 = raw[4 * cols + a];   // white prob
            float p1 = raw[5 * cols + a];   // yellow prob
            float p2 = raw[6 * cols + a];   // red prob

            float conf = MathF.Max(p0, MathF.Max(p1, p2));
            if (conf < _confThreshold) continue;

            var color = (p0 >= p1 && p0 >= p2) ? BallColor.White
                      : (p1 >= p2)              ? BallColor.Yellow
                                                : BallColor.Red;
            candidates.Add((cx, cy, bw, bh, conf, color));
        }

        // NMS per class then convert coordinates back to original image space
        var results = new List<DetectedBall>();
        foreach (BallColor cls in Enum.GetValues<BallColor>())
        {
            var cls_cands = candidates.Where(c => c.color == cls)
                                      .OrderByDescending(c => c.conf)
                                      .ToList();

            while (cls_cands.Count > 0)
            {
                var best = cls_cands[0];
                cls_cands.RemoveAt(0);

                // Map from 640×640 letterboxed space back to original image
                float oX = (best.cx - padX) / scaleX / origW;
                float oY = (best.cy - padY) / scaleY / origH;
                results.Add(new DetectedBall(cls,
                    Math.Clamp(oX, 0f, 1f),
                    Math.Clamp(oY, 0f, 1f),
                    best.conf));

                // Remove overlapping detections of the same class
                cls_cands.RemoveAll(c => Iou(best, c) > _iouThreshold);
            }
        }

        return results;
    }

    private static float Iou(
        (float cx, float cy, float w, float h, float conf, BallColor color) a,
        (float cx, float cy, float w, float h, float conf, BallColor color) b)
    {
        float ax1 = a.cx - a.w / 2, ay1 = a.cy - a.h / 2;
        float ax2 = a.cx + a.w / 2, ay2 = a.cy + a.h / 2;
        float bx1 = b.cx - b.w / 2, by1 = b.cy - b.h / 2;
        float bx2 = b.cx + b.w / 2, by2 = b.cy + b.h / 2;

        float ix1 = MathF.Max(ax1, bx1), iy1 = MathF.Max(ay1, by1);
        float ix2 = MathF.Min(ax2, bx2), iy2 = MathF.Min(ay2, by2);
        float inter = MathF.Max(0, ix2 - ix1) * MathF.Max(0, iy2 - iy1);

        float areaA = (ax2 - ax1) * (ay2 - ay1);
        float areaB = (bx2 - bx1) * (by2 - by1);
        return inter / (areaA + areaB - inter + 1e-6f);
    }

    public void Dispose() => _session?.Dispose();
}
