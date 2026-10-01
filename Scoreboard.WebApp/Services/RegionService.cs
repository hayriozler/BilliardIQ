using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class RegionService(DataContext db)
{
    public Task<List<Region>> ListAsync(int organizationId) =>
        db.RegionSet
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.DeletedAt == null)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<Region> UpsertAsync(int organizationId, int id, string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Bölge adı gerekli.");
        if (name.Length > 150) throw new ArgumentException("Bölge adı en fazla 150 karakter olabilir.");

        var region = id != 0
            ? await db.RegionSet.FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId && a.DeletedAt == null)
            : null;
        if (id != 0 && region is null) throw new ArgumentException("Bölge bulunamadı.");

        if (await db.RegionSet.AnyAsync(a =>
                a.OrganizationId == organizationId && a.DeletedAt == null && a.Id != id && a.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException("Bu isimde bir bölge zaten var.");
        }

        if (region is null)
        {
            region = new Region { OrganizationId = organizationId };
            db.RegionSet.Add(region);
        }

        region.Name = name;
        await db.SaveChangesAsync();
        return region;
    }

    public async Task DeleteAsync(int organizationId, int id)
    {
        var region = await db.RegionSet.FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == organizationId && a.DeletedAt == null)
            ?? throw new InvalidOperationException("Bölge bulunamadı.");
        if (await db.PlayerSet.AnyAsync(p => p.RegionId == id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan bölge silinemez. Önce oyuncuların bölgesini değiştirin.");
        }

        region.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
