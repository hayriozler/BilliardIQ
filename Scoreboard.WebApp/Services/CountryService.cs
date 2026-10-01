using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class CountryService(DataContext db)
{
    public static Country Localized(Country country)
    {
        if (CountryCatalog.Find(country.Code) is { } entry)
        {
            country.Name = entry.NameIn(Loc.Current);
        }

        return country;
    }

    public async Task<List<Country>> ListAsync(int organizationId) =>
        (await db.OrganizationCountrySet
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.DeletedAt == null)
            .Select(x => x.Country)
            .ToListAsync())
        .Select(Localized)
        .OrderBy(c => c.Name)
        .ToList();

    public async Task<List<Country>> AvailableAsync(int organizationId)
    {
        var selected = db.OrganizationCountrySet.Where(x => x.OrganizationId == organizationId && x.DeletedAt == null).Select(x => x.CountryId);
        return (await db.CountrySet.AsNoTracking().Where(c => !selected.Contains(c.Id)).ToListAsync())
            .Select(Localized)
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task AddAsync(int organizationId, int countryId)
    {
        if (!await db.CountrySet.AnyAsync(c => c.Id == countryId))
        {
            throw new ArgumentException("Katalogda ülke bulunamadı.");
        }

        if (await db.OrganizationCountrySet.AnyAsync(x => x.OrganizationId == organizationId && x.CountryId == countryId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bu ülke zaten ekli.");
        }

        db.OrganizationCountrySet.Add(new OrganizationCountry { OrganizationId = organizationId, CountryId = countryId });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int organizationId, int countryId)
    {
        var link = await db.OrganizationCountrySet.FirstOrDefaultAsync(x => x.CountryId == countryId && x.OrganizationId == organizationId && x.DeletedAt == null)
            ?? throw new InvalidOperationException("Ülke bulunamadı.");
        if (await db.OrganizationCitySet.AnyAsync(x => x.City.CountryId == countryId && x.DeletedAt == null))
        {
            throw new InvalidOperationException("Şehri olan ülke silinemez. Önce şehirleri silin.");
        }

        if (await db.OrganizationRegionSet.AnyAsync(x => x.Region.CountryId == countryId && x.DeletedAt == null))
        {
            throw new InvalidOperationException("Bölgesi olan ülke silinemez. Önce bölgeleri silin.");
        }

        if (await db.PlayerSet.AnyAsync(p => p.CountryId == countryId && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan ülke silinemez. Önce oyuncuların ülkesini değiştirin.");
        }

        link.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
