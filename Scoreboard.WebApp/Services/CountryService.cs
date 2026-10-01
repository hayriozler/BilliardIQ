using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>The salon's list of countries that players can be assigned to.</summary>
public class CountryService(DataContext db)
{
    public Task<List<Country>> ListAsync(int organizationId) =>
        db.CountrySet
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Country> UpsertAsync(int organizationId, int id, string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Ülke adı gerekli.");
        if (name.Length > 100) throw new ArgumentException("Ülke adı en fazla 100 karakter olabilir.");

        var country = id != 0
            ? await db.CountrySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            : null;
        if (id != 0 && country is null) throw new ArgumentException("Ülke bulunamadı.");

        if (await db.CountrySet.AnyAsync(c =>
                c.OrganizationId == organizationId && c.DeletedAt == null && c.Id != id && c.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException("Bu isimde bir ülke zaten var.");
        }

        if (country is null)
        {
            country = new Country { OrganizationId = organizationId };
            db.CountrySet.Add(country);
        }

        var renamed = country.Id != 0 && country.Name != name;
        country.Name = name;
        if (renamed)
        {
            // Players keep the country name as text for API clients; keep it in step.
            foreach (var player in await db.PlayerSet.Where(p => p.CountryId == country.Id).ToListAsync())
            {
                player.Nationality = name;
            }
        }

        await db.SaveChangesAsync();
        return country;
    }

    public async Task DeleteAsync(int organizationId, int id)
    {
        var country = await db.CountrySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Ülke bulunamadı.");
        if (await db.CitySet.AnyAsync(c => c.CountryId == id && c.DeletedAt == null))
        {
            throw new InvalidOperationException("Şehri olan ülke silinemez. Önce şehirleri silin.");
        }

        if (await db.PlayerSet.AnyAsync(p => p.CountryId == id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan ülke silinemez. Önce oyuncuların ülkesini değiştirin.");
        }

        country.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
