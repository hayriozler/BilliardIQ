using SkiaSharp;
using Svg.Skia;

namespace BilliardIQ.Mobile.Services;

public static class AvatarRenderer
{
    /// <summary>
    /// Rasterizes a bundled avatar SVG (from Resources/Raw) into a square PNG, so preset
    /// avatars can be sent to the scoreboard the same way a camera photo is (photoBase64/photoExtension)
    /// instead of relying on the Pi generating its own art for the "avatar" seed string.
    /// </summary>
    public static async Task<byte[]> RenderPngAsync(string svgFileName, int size = 300)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync(svgFileName);
        using var svg = new SKSvg();
        var picture = svg.Load(stream);
        if (picture is null || picture.CullRect.Width <= 0 || picture.CullRect.Height <= 0) return [];

        var bounds = picture.CullRect;
        var scale = Math.Min(size / bounds.Width, size / bounds.Height);

        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Translate((size - bounds.Width * scale) / 2f, (size - bounds.Height * scale) / 2f);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? [];
    }
}
