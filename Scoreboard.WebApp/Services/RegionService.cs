using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class RegionService(DataContext db)
{
    public async Task<List<Region>> ListAsync(int organizationId) =>
        (await db.OrganizationRegionSet
            .AsNoTracking()
            .Include(x => x.Region).ThenInclude(r => r.Country)
            .Where(x => x.OrganizationId == organizationId && x.DeletedAt == null)
            .Select(x => x.Region)
            .ToListAsync())
        .Select(r => { CountryService.Localized(r.Country); return r; })
        .OrderBy(r => r.Country.Name).ThenBy(r => r.Name)
        .ToList();

    public async Task<List<Region>> AvailableAsync(int organizationId)
    {
        var selected = db.OrganizationRegionSet.Where(x => x.OrganizationId == organizationId && x.DeletedAt == null).Select(x => x.RegionId);
        var linkedCountries = db.OrganizationCountrySet.Where(x => x.OrganizationId == organizationId && x.DeletedAt == null).Select(x => x.CountryId);
        return (await db.RegionSet.AsNoTracking().Include(r => r.Country)
                .Where(r => linkedCountries.Contains(r.CountryId) && !selected.Contains(r.Id))
                .ToListAsync())
            .Select(r => { CountryService.Localized(r.Country); return r; })
            .OrderBy(r => r.Country.Name).ThenBy(r => r.Name)
            .ToList();
    }

    public async Task AddAsync(int organizationId, int regionId)
    {
        var region = await db.RegionSet.AsNoTracking().FirstOrDefaultAsync(r => r.Id == regionId)
            ?? throw new ArgumentException("Katalogda bölge bulunamadı.");
        if (!await db.OrganizationCountrySet.AnyAsync(x => x.OrganizationId == organizationId && x.CountryId == region.CountryId && x.DeletedAt == null))
        {
            throw new ArgumentException("Önce bölgenin ülkesini ekleyin.");
        }

        if (await db.OrganizationRegionSet.AnyAsync(x => x.OrganizationId == organizationId && x.RegionId == regionId && x.DeletedAt == null))
        {
            throw new ArgumentException("Bu bölge zaten ekli.");
        }

        db.OrganizationRegionSet.Add(new OrganizationRegion { OrganizationId = organizationId, RegionId = regionId });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int organizationId, int regionId)
    {
        var link = await db.OrganizationRegionSet.FirstOrDefaultAsync(x => x.RegionId == regionId && x.OrganizationId == organizationId && x.DeletedAt == null)
            ?? throw new InvalidOperationException("Bölge bulunamadı.");
        if (await db.PlayerSet.AnyAsync(p => p.RegionId == regionId && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan bölge silinemez. Önce oyuncuların bölgesini değiştirin.");
        }

        link.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
