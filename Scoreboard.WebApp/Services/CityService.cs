using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class CityService(DataContext db)
{
    public async Task<List<City>> ListAsync(int organizationId, int? countryId = null) =>
        (await db.OrganizationCitySet
            .AsNoTracking()
            .Include(x => x.City).ThenInclude(c => c.Country)
            .Where(x => x.OrganizationId == organizationId && x.DeletedAt == null && (countryId == null || x.City.CountryId == countryId))
            .Select(x => x.City)
            .ToListAsync())
        .Select(c => { CountryService.Localized(c.Country); return c; })
        .OrderBy(c => c.Country.Name).ThenBy(c => c.Name)
        .ToList();

    public async Task<List<City>> AvailableAsync(int organizationId, int countryId)
    {
        var selected = db.OrganizationCitySet.Where(x => x.OrganizationId == organizationId && x.DeletedAt == null).Select(x => x.CityId);
        return await db.CitySet.AsNoTracking()
            .Where(c => c.CountryId == countryId && !selected.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task AddAsync(int organizationId, int cityId)
    {
        var city = await db.CitySet.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cityId)
            ?? throw new ArgumentException("Katalogda şehir bulunamadı.");
        if (!await db.OrganizationCountrySet.AnyAsync(x => x.OrganizationId == organizationId && x.CountryId == city.CountryId && x.DeletedAt == null))
        {
            throw new ArgumentException("Önce şehrin ülkesini ekleyin.");
        }

        if (await db.OrganizationCitySet.AnyAsync(x => x.OrganizationId == organizationId && x.CityId == cityId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bu şehir zaten ekli.");
        }

        db.OrganizationCitySet.Add(new OrganizationCity { OrganizationId = organizationId, CityId = cityId });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int organizationId, int cityId)
    {
        var link = await db.OrganizationCitySet.FirstOrDefaultAsync(x => x.CityId == cityId && x.OrganizationId == organizationId && x.DeletedAt == null)
            ?? throw new InvalidOperationException("Şehir bulunamadı.");
        if (await db.PlayerSet.AnyAsync(p => p.CityId == cityId && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan şehir silinemez. Önce oyuncuların şehrini değiştirin.");
        }

        link.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
