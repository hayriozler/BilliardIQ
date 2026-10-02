using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scoreboard.Common;

public static class AvatarGenerator
{
    private sealed record Palette(string Bg, string Bg2, string C1, string C2, string C3);

    private sealed record Animal(string Name, string Back, string Half, string Front);

    private sealed record Catalog(
        [property: JsonPropertyName("palettes")] List<PaletteDto> Palettes,
        [property: JsonPropertyName("animals")] List<AnimalDto> Animals);

    private sealed record PaletteDto(string Name, string Bg, string Bg2, string C1, string C2, string C3);

    private sealed record AnimalDto(string Name, string Back, string Half, string Front);

    private static readonly List<Palette> _palettes;
    private static readonly List<Animal> _animals;

    static AvatarGenerator()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Avatars.animals.json")
            ?? throw new InvalidOperationException("Embedded avatar catalog 'Avatars.animals.json' is missing.");
        var catalog = JsonSerializer.Deserialize<Catalog>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Avatar catalog is empty.");
        _palettes = catalog.Palettes.Select(p => new Palette(p.Bg, p.Bg2, p.C1, p.C2, p.C3)).ToList();
        _animals = catalog.Animals.Select(a => new Animal(a.Name, a.Back, a.Half, a.Front)).ToList();
    }

    public static int Count => _animals.Count * _palettes.Count;

    public static string GetColor(int seed) => Resolve(seed).Palette.C1;

    public static string ToSvg(int seed, int size = 64)
    {
        var (animal, palette) = Resolve(seed);
        string Paint(string shapes) => shapes.Replace("{c1}", palette.C1).Replace("{c2}", palette.C2).Replace("{c3}", palette.C3);

        var half = Paint(animal.Half);
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 64 64' width='{size}' height='{size}'>");
        sb.Append(CultureInfo.InvariantCulture, $"<defs><radialGradient id='g' cx='50%' cy='35%' r='75%'><stop offset='0' stop-color='{palette.Bg2}'/><stop offset='1' stop-color='{palette.Bg}'/></radialGradient></defs>");
        sb.Append("<rect width='64' height='64' rx='11' fill='url(#g)'/>");
        sb.Append(Paint(animal.Back));
        sb.Append("<g>").Append(half).Append("</g>");
        sb.Append("<g transform='matrix(-1 0 0 1 64 0)'>").Append(half).Append("</g>");
        sb.Append(Paint(animal.Front));
        sb.Append("</svg>");
        return sb.ToString();
    }

    public static string ToDataUri(int seed, int size = 64)
    {
        var bytes = Encoding.UTF8.GetBytes(ToSvg(seed, size));
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(bytes)}";
    }

    private static (Animal Animal, Palette Palette) Resolve(int seed)
    {
        var id = Math.Abs(seed) % Count;
        return (_animals[id % _animals.Count], _palettes[id / _animals.Count % _palettes.Count]);
    }
}
