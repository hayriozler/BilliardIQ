using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Scoreboard.WebApp.Services;

public sealed class Loc
{
    public const string DefaultLanguage = "tr";

    public static readonly IReadOnlyList<(string Code, string Name)> Languages =
    [
        ("tr", "Türkçe"),
        ("en", "English"),
        ("nl", "Nederlands")
    ];

    private static readonly Dictionary<string, Dictionary<string, string>> Catalogs = LoadCatalogs();

    public static string Current => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public static string Translate(string key)
    {
        if (Current != DefaultLanguage &&
            Catalogs.TryGetValue(Current, out var catalog) &&
            catalog.TryGetValue(key, out var value))
        {
            return value;
        }

        return key;
    }

    public static string Translate(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Translate(key), args);

    public static string TranslateTo(string language, string key, params object?[] args)
    {
        var text = language != DefaultLanguage && Catalogs.TryGetValue(language, out var catalog) && catalog.TryGetValue(key, out var value)
            ? value
            : key;
        return string.Format(CultureInfo.InvariantCulture, text, args);
    }

    public static bool IsSupported(string? language) => Languages.Any(l => l.Code == language);

    public string this[string key] => Translate(key);

    public string this[string key, params object?[] args] => Translate(key, args);

    public string Error(Exception ex) =>
        ex is LocalizedArgumentException localized ? Translate(localized.Key, localized.Args) : Translate(ex.Message);

    private static Dictionary<string, Dictionary<string, string>> LoadCatalogs()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, _) in Languages.Where(l => l.Code != DefaultLanguage))
        {
            var merged = new Dictionary<string, string>();
            foreach (var name in assembly.GetManifestResourceNames()
                         .Where(n => n.StartsWith($"Localization.{code}.", StringComparison.OrdinalIgnoreCase) && n.EndsWith(".json"))
                         .OrderBy(n => n, StringComparer.Ordinal))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                foreach (var (key, value) in JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [])
                {
                    merged[key] = value;
                }
            }

            if (merged.Count > 0) result[code] = merged;
        }

        return result;
    }
}

public sealed class LocalizedArgumentException(string key, params object[] args)
    : ArgumentException(string.Format(CultureInfo.GetCultureInfo("tr-TR"), key, args))
{
    public string Key { get; } = key;

    public object[] Args { get; } = args;
}
