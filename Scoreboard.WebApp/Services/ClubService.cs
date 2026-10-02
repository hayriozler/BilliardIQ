using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class ClubService(DataContext db)
{
    public Task<List<Club>> ListAsync() =>
        db.ClubSet
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public Task<List<Club>> ListByIdsAsync(IReadOnlyCollection<int> ids) =>
        db.ClubSet.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync();

    public Task<Club?> GetAsync(int id) =>
        db.ClubSet.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);

    public async Task<Club> UpsertAsync(int id, string name, string? shortName, string? city, string? primaryColor)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Kulüp adı gerekli.");
        }

        var club = id != 0 ? await GetAsync(id) : null;
        if (club is null)
        {
            club = new Club { OrganizationId = db.CurrentOrganizationId };
            db.ClubSet.Add(club);
        }

        club.Name = name;
        club.ShortName = string.IsNullOrWhiteSpace(shortName) ? DefaultShortName(name) : shortName.Trim().ToUpperInvariant();
        club.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        club.PrimaryColor = string.IsNullOrWhiteSpace(primaryColor) ? null : primaryColor.Trim();
        await db.SaveChangesAsync();
        return club;
    }

    public async Task<Club> EnsureDefaultClubAsync()
    {
        var club = await db.ClubSet
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();
        if (club is not null)
        {
            return club;
        }

        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == db.CurrentOrganizationId);
        return await UpsertAsync(0, organization.Name, null, null, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var club = await GetAsync(id);
        if (club is null)
        {
            return false;
        }

        if (await db.TeamSet.AnyAsync(t => t.ClubId == id && t.DeletedAt == null))
        {
            throw new InvalidOperationException("Takımı olan kulüp silinemez. Önce takımları silin.");
        }

        club.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    private static string DefaultShortName(string name)
    {
        var letters = new string(name.Where(char.IsLetterOrDigit).Take(4).ToArray());
        return letters.Length == 0 ? "KLB" : letters.ToUpperInvariant();
    }
}
