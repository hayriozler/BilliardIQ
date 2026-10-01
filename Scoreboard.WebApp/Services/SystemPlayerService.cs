using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>
/// The two default players ("Player 1" and "Player 2") that a scoreboard uses when nobody is picked. They are shared by all
/// venues, always have Id 1 and 2 (here, in the API and on every scoreboard), and can be neither edited nor deleted. Their
/// stored names are the source language; every reader shows them in its own language (the API in the venue's language,
/// the panel in the user's, a scoreboard in its own), so a language change shows up everywhere without touching the rows.
/// </summary>
public class SystemPlayerService(DataContext db)
{
    public const string NameKey = "Oyuncu {0}";

    public static string NameFor(int slot, string language) => Loc.TranslateTo(language, NameKey, slot);

    /// <summary>Makes sure the two rows exist, and that real players never get Id 1 or 2.</summary>
    public async Task EnsureAllAsync()
    {
        foreach (var slot in new[] { 1, 2 })
        {
            var name = NameFor(slot, Loc.DefaultLanguage);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PlayerSet" ("Id", "FirstName", "LastName", "DisplayName", "IsGuest", "IsPublicProfile", "Level",
                                         "IsSystem", "SystemSlot", "CreatedAt", "UpdatedAt")
                VALUES ({slot}, {name}, '', {name}, TRUE, FALSE, {(int)Level.Intermidiate}, TRUE, {slot}, now(), now())
                ON CONFLICT ("Id") DO NOTHING
                """);
        }

        // the identity counter must be past the reserved ids
        await db.Database.ExecuteSqlRawAsync(
            """SELECT setval(pg_get_serial_sequence('"PlayerSet"', 'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM "PlayerSet"), 2))""");
    }
}
