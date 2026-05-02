using SkiaSharp;

namespace BilliardIQ.Mobile.Services;

public static class ImagePreprocessor
{
    /// <summary>
    /// Decodes any supported image format (JPEG, PNG, WebP, HEIC on Android 10+ / iOS)
    /// and re-encodes as JPEG so ML Kit OCR always receives a decodable format.
    /// </summary>
    public static byte[] NormalizeToJpeg(byte[] imageBytes)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap is null || bitmap.IsNull) return imageBytes;
            using var image = SKImage.FromBitmap(bitmap);
            using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 92);
            return data?.ToArray() ?? imageBytes;
        }
        catch { return imageBytes; }
    }

    /// <summary>
    /// Creates a small thumbnail suitable for storing in the database (400×300 max, JPEG 75%).
    /// Returns an empty array if decoding fails.
    /// </summary>
    public static byte[] CreateThumbnail(byte[] imageBytes, int maxWidth = 400, int maxHeight = 300)
    {
        try
        {
            using var original = SKBitmap.Decode(imageBytes);
            if (original is null || original.IsNull) return [];

            float scale = Math.Min((float)maxWidth / original.Width, (float)maxHeight / original.Height);
            int   w     = Math.Max(1, (int)(original.Width  * scale));
            int   h     = Math.Max(1, (int)(original.Height * scale));

            using var resized = original.Resize(new SKImageInfo(w, h), SKFilterQuality.Medium);
            if (resized is null) return [];

            using var image = SKImage.FromBitmap(resized);
            using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 75);
            return data?.ToArray() ?? [];
        }
        catch { return []; }
    }
}
