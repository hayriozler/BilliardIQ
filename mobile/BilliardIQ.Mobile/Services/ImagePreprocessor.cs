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
            using var original = DecodeWithExifOrientation(imageBytes);
            if (original is null || original.IsNull) return [];

            float scale = Math.Min((float)maxWidth / original.Width, (float)maxHeight / original.Height);
            int   w     = Math.Max(1, (int)(original.Width  * scale));
            int   h     = Math.Max(1, (int)(original.Height * scale));

            using var resized = original.Resize(new SKImageInfo(w, h), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            if (resized is null) return [];

            using var image = SKImage.FromBitmap(resized);
            using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 75);
            return data?.ToArray() ?? [];
        }
        catch { return []; }
    }

    /// <summary>
    /// Decodes an image and bakes in its EXIF orientation. SKBitmap.Decode ignores the EXIF
    /// orientation tag, so photos captured in portrait on Android/iOS come out sideways unless
    /// this correction is applied.
    /// </summary>
    private static SKBitmap? DecodeWithExifOrientation(byte[] imageBytes)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(imageBytes));
        var bitmap = SKBitmap.Decode(imageBytes);
        if (bitmap is null || bitmap.IsNull || codec is null) return bitmap;

        switch (codec.EncodedOrigin)
        {
            case SKEncodedOrigin.BottomRight:
            {
                var rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                using var canvas = new SKCanvas(rotated);
                canvas.RotateDegrees(180, bitmap.Width / 2f, bitmap.Height / 2f);
                canvas.DrawBitmap(bitmap, 0, 0);
                bitmap.Dispose();
                return rotated;
            }
            case SKEncodedOrigin.RightTop:
            {
                var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                using var canvas = new SKCanvas(rotated);
                canvas.Translate(rotated.Width, 0);
                canvas.RotateDegrees(90);
                canvas.DrawBitmap(bitmap, 0, 0);
                bitmap.Dispose();
                return rotated;
            }
            case SKEncodedOrigin.LeftBottom:
            {
                var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                using var canvas = new SKCanvas(rotated);
                canvas.Translate(0, rotated.Height);
                canvas.RotateDegrees(270);
                canvas.DrawBitmap(bitmap, 0, 0);
                bitmap.Dispose();
                return rotated;
            }
            default:
                return bitmap;
        }
    }
}
