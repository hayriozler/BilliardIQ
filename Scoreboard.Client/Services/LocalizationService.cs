using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Scoreboard.Client.Services;

public class LocalizationService(DataContext db)
{
    public static readonly Dictionary<string, Dictionary<string, string>> Values = Load();
    public event Action? LanguageChanged;

    public void SetLanguage(string language)
    {
        SetLang(language);
        LanguageChanged?.Invoke();
    }
    // not tracked: the language can be changed by another context (the server sync), and a tracked copy would be stale
    public string GetLang => db.SettingsSet.AsNoTracking().First(p => p.Id == "Lang").Value;
    private void SetLang(string language)
    {
        var setting = db.SettingsSet.First(p => p.Id == "Lang");
        setting.Value = language;
        db.SaveChanges();
    }
    public string T(string key) => Get(GetLang, key);
    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        string rootpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Localization");
        foreach (var file in Directory.GetFiles(rootpath, "*.json"))
        {
            var language = Path.GetFileNameWithoutExtension(file);
            var json = File.ReadAllText(file);
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            result[language] = values;
        }
        return result;
    }

    public string Get(string language, string key)
    {
        if (Values.TryGetValue(language, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        return Values[GetLang].GetValueOrDefault(key, key);
    }
}

