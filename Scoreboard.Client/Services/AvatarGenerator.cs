using System.Globalization;
using System.Text;

namespace Scoreboard.Client.Services;

public static class AvatarGenerator
{
    public const int Count = 100;

    private static readonly string[][] _palettes =
    [
        ["#FFD6A5", "#FF934F", "#2D3142"],
        ["#A0E7E5", "#00B4D8", "#03045E"],
        ["#B8F2E6", "#5E548E", "#22223B"],
        ["#FFC6FF", "#BDB2FF", "#4C3B6E"],
        ["#FFADAD", "#FF6B6B", "#5C1A1A"],
        ["#CAFFBF", "#57CC99", "#1B4332"],
        ["#FDFFB6", "#FFD60A", "#6B5B00"],
        ["#9BF6FF", "#48CAE4", "#023047"],
        ["#FFC8DD", "#FFAFCC", "#6D2E46"],
        ["#D0F4DE", "#95D5B2", "#1B4332"],
    ];

    public static string GetColor(int seed) => _palettes[Math.Abs(seed) % _palettes.Length][1];

    public static int SeedFromName(string name)
    {
        var hash = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(name))
        {
            hash = (hash ^ b) * 16777619u;
        }

        return (int)(hash % Count);
    }

    public static string ToSvg(int seed, int size = 64)
    {
        var rnd = new Random(seed);
        var palette = _palettes[Math.Abs(seed) % _palettes.Length];
        var bg = palette[0];
        var face = palette[1];
        var accent = palette[2];

        var faceR = size * (0.3 + rnd.NextDouble() * 0.08);
        var cx = size / 2.0 + (rnd.NextDouble() - 0.5) * size * 0.1;
        var cy = size / 2.0 + (rnd.NextDouble() - 0.5) * size * 0.1;

        var eyeDx = faceR * (0.36 + rnd.NextDouble() * 0.12);
        var eyeDy = -faceR * 0.1;
        var eyeR = faceR * (0.09 + rnd.NextDouble() * 0.05);

        var mouthY = cy + faceR * 0.34;
        var mouthWidth = faceR * (0.45 + rnd.NextDouble() * 0.2);
        var smile = rnd.NextDouble() > 0.25;
        var mouthCurve = smile ? faceR * 0.3 : -faceR * 0.05;
        var mouthPath = FormattableString.Invariant(
            $"M {cx - mouthWidth / 2:0.##} {mouthY:0.##} Q {cx:0.##} {mouthY + mouthCurve:0.##} {cx + mouthWidth / 2:0.##} {mouthY:0.##}");

        var cheeks = rnd.NextDouble() > 0.5;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {size} {size}' width='{size}' height='{size}'>");
        sb.Append(CultureInfo.InvariantCulture, $"<rect width='{size}' height='{size}' rx='{size * 0.18:0.##}' fill='{bg}' />");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx='{cx:0.##}' cy='{cy:0.##}' r='{faceR:0.##}' fill='{face}' />");

        if (cheeks)
        {
            var cheekR = faceR * 0.14;
            var cheekDx = faceR * 0.62;
            var cheekY = cy + faceR * 0.12;
            sb.Append(CultureInfo.InvariantCulture, $"<circle cx='{cx - cheekDx:0.##}' cy='{cheekY:0.##}' r='{cheekR:0.##}' fill='{accent}' opacity='0.25' />");
            sb.Append(CultureInfo.InvariantCulture, $"<circle cx='{cx + cheekDx:0.##}' cy='{cheekY:0.##}' r='{cheekR:0.##}' fill='{accent}' opacity='0.25' />");
        }

        sb.Append(CultureInfo.InvariantCulture, $"<circle cx='{cx - eyeDx:0.##}' cy='{cy + eyeDy:0.##}' r='{eyeR:0.##}' fill='{accent}' />");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx='{cx + eyeDx:0.##}' cy='{cy + eyeDy:0.##}' r='{eyeR:0.##}' fill='{accent}' />");
        sb.Append(CultureInfo.InvariantCulture, $"<path d='{mouthPath}' stroke='{accent}' stroke-width='{faceR * 0.09:0.##}' fill='none' stroke-linecap='round' />");
        sb.Append("</svg>");

        return sb.ToString();
    }

    public static string ToDataUri(int seed, int size = 64)
    {
        var bytes = Encoding.UTF8.GetBytes(ToSvg(seed, size));
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
    }
}
