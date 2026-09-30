using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Scoreboard.WebApp.Services;

/// <summary>
/// UI translations. Turkish is the source language: the Turkish text itself is the key and is returned
/// unchanged for "tr" (and for any text without a translation). Other languages live in
/// <c>Localization/{code}.json</c> (flat key → translation) and are embedded in the assembly.
/// To add a language: drop a new JSON file in <c>Localization/</c> and add it to <see cref="Languages"/>.
/// </summary>
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

    /// <summary>Translation into a specific language, independent of the current request culture.</summary>
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

    /// <summary>Message of a service exception in the current language.</summary>
    public string Error(Exception ex) =>
        ex is LocalizedArgumentException localized ? Translate(localized.Key, localized.Args) : Translate(ex.Message);

    private static Dictionary<string, Dictionary<string, string>> LoadCatalogs()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, _) in Languages.Where(l => l.Code != DefaultLanguage))
        {
            using var stream = assembly.GetManifestResourceStream($"Localization.{code}.json");
            if (stream is null) continue;
            result[code] = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
        }

        return result;
    }
}

/// <summary>A validation error whose text is translated at display time (Turkish template + arguments).</summary>
public sealed class LocalizedArgumentException(string key, params object[] args)
    : ArgumentException(string.Format(CultureInfo.GetCultureInfo("tr-TR"), key, args))
{
    public string Key { get; } = key;

    public object[] Args { get; } = args;
}
