using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class AssociationService(DataContext db)
{
    public Task<List<Association>> ListAsync() =>
        db.AssociationSet
            .AsNoTracking()
            .Where(a => a.DeletedAt == null)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<Association> UpsertAsync(int id, string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Dernek / federasyon adı gerekli.");
        if (name.Length > 150) throw new ArgumentException("Dernek adı en fazla 150 karakter olabilir.");

        var association = id != 0
            ? await db.AssociationSet.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null)
            : null;
        if (id != 0 && association is null) throw new ArgumentException("Dernek / federasyon bulunamadı.");

        if (await db.AssociationSet.AnyAsync(a =>
                a.DeletedAt == null && a.Id != id && a.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException("Bu isimde bir dernek / federasyon zaten var.");
        }

        if (association is null)
        {
            association = new Association { OrganizationId = db.CurrentOrganizationId };
            db.AssociationSet.Add(association);
        }

        association.Name = name;
        await db.SaveChangesAsync();
        return association;
    }

    public async Task DeleteAsync(int id)
    {
        var association = await db.AssociationSet.FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null)
            ?? throw new InvalidOperationException("Dernek / federasyon bulunamadı.");
        if (await db.PlayerSet.AnyAsync(p => p.AssociationId == id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Oyuncusu olan dernek / federasyon silinemez. Önce oyuncuların bağlantısını değiştirin.");
        }

        association.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }
}
