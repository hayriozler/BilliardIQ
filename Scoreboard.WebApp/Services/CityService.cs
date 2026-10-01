using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class CityService(DataContext db)
{
    public Task<List<City>> ListAsync(int organizationId, int? countryId = null) =>
        db.CitySet
            .AsNoTracking()
            .Include(c => c.Country)
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null && (countryId == null || c.CountryId == countryId))
            .OrderBy(c => c.Country.Name).ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<City> UpsertAsync(int organizationId, int id, int countryId, string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Şehir adı gerekli.");
        if (name.Length > 100) throw new ArgumentException("Şehir adı en fazla 100 karakter olabilir.");

        var country = await db.CountrySet.FirstOrDefaultAsync(c => c.Id == countryId && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new ArgumentException("Ülke seçin.");

        var city = id != 0
            ? await db.CitySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            : null;
        if (id != 0 && city is null) throw new ArgumentException("Şehir bulunamadı.");

        if (await db.CitySet.AnyAsync(c =>
                c.OrganizationId == organizationId && c.DeletedAt == null && c.Id != id && c.CountryId == country.Id && c.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException("Bu ülkede bu isimde bir şehir zaten var.");
        }

        if (city is null)
        {
            city = new City { OrganizationId = organizationId };
            db.CitySet.Add(city);
        }
        else if (city.CountryId != country.Id &&
                 await db.PlayerSet.AnyAsync(p => p.CityId == city.Id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan şehrin ülkesi değiştirilemez.");
        }

        var renamed = city.Id != 0 && city.Name != name;
        city.Name = name;
        city.CountryId = country.Id;
        if (renamed)
        {
            foreach (var player in await db.PlayerSet.Where(p => p.CityId == city.Id).ToListAsync())
            {
                player.City = name;
            }
        }

        await db.SaveChangesAsync();
        return city;
    }

    public async Task DeleteAsync(int organizationId, int id)
    {
        var city = await db.CitySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Şehir bulunamadı.");
        if (await db.PlayerSet.AnyAsync(p => p.CityId == id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan şehir silinemez. Önce oyuncuların şehrini değiştirin.");
        }

        city.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
