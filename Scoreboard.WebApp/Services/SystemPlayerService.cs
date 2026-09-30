using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>
/// Every salon owns two undeletable, uneditable players ("Player 1" and "Player 2") that the scoreboard uses when
/// no real player is picked. Their names follow the salon's language.
/// </summary>
public class SystemPlayerService(DataContext db)
{
    public const string NameKey = "Oyuncu {0}";

    /// <summary>Creates whichever of the two system players the salon is missing.</summary>
    public async Task EnsureAsync(Organization organization)
    {
        var existing = await db.PlayerSet
            .Where(p => p.CreatedInOrganizationId == organization.Id && p.IsSystem)
            .Select(p => p.SystemSlot)
            .ToListAsync();

        foreach (var slot in new[] { 1, 2 }.Where(s => !existing.Contains(s)))
        {
            var name = Loc.TranslateTo(organization.Language, NameKey, slot);
            db.PlayerSet.Add(new Player
            {
                CreatedInOrganizationId = organization.Id,
                FirstName = name,
                LastName = "",
                DisplayName = name,
                IsSystem = true,
                SystemSlot = slot,
                IsGuest = true
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Gives salons created before system players existed their two players.</summary>
    public async Task EnsureAllAsync()
    {
        foreach (var organization in await db.OrganizationSet.Where(o => o.DeletedAt == null).ToListAsync())
        {
            await EnsureAsync(organization);
        }
    }
}
