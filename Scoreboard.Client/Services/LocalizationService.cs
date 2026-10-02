using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Scoreboard.Client.Services;

public class LocalizationService(DataContext db)
{
    public static readonly Dictionary<string, Dictionary<string, string>> Values = Load();

    public void SetLanguage(string language) => SetLang(language);
    private static volatile string? _cachedLang;

    public static void InvalidateLanguage() => _cachedLang = null;

    public string GetLang => _cachedLang ??= db.SettingsSet.AsNoTracking().First(p => p.Id == "Lang").Value;
    private void SetLang(string language)
    {
        var setting = db.SettingsSet.First(p => p.Id == "Lang");
        setting.Value = language;
        db.SaveChanges();
        InvalidateLanguage();
        RenameSystemPlayers(db, language);
    }

    public static void RenameSystemPlayers(DataContext db, string language)
    {
        foreach (var slot in new[] { 1, 2 })
        {
            if (Values.TryGetValue(language, out var labels) && labels.TryGetValue($"Player{slot}Label", out var label))
            {
                db.PlayerSet
                    .Where(p => p.IsSystem && p.SystemSlot == slot)
                    .ExecuteUpdate(s => s.SetProperty(p => p.Name, label).SetProperty(p => p.Nickname, label));
            }
        }
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

