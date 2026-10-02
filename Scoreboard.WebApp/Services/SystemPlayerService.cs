using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class SystemPlayerService(DataContext db)
{
    public const string NameKey = "Oyuncu {0}";

    public static string NameFor(int slot, string language) => Loc.TranslateTo(language, NameKey, slot);

    public async Task NotifyLanguageChangedAsync(int organizationId)
    {
        foreach (var slot in new[] { 1, 2 })
        {
            await db.Database.ExecuteSqlInterpolatedAsync(EntityChangeSql.Upsert(organizationId, nameof(Player), slot, false));
        }
    }

    public async Task EnsureAllAsync()
    {
        foreach (var slot in new[] { 1, 2 })
        {
            var name = NameFor(slot, Loc.DefaultLanguage);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Player" ("Id", "FirstName", "LastName", "DisplayName", "IsGuest", "IsPublicProfile", "Level",
                                         "IsSystem", "SystemSlot", "CreatedAt", "UpdatedAt")
                VALUES ({slot}, {name}, '', {name}, TRUE, FALSE, {(int)Level.Intermidiate}, TRUE, {slot}, now(), now())
                ON CONFLICT ("Id") DO NOTHING
                """);
        }

        await db.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('"Player"', 'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM "Player"), 2))""");
    }
}
