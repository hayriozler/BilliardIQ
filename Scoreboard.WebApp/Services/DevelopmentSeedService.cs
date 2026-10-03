using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class DevelopmentSeedService(DataContext db, OrganizationRunner runner, ILogger<DevelopmentSeedService> logger)
{
    public async Task EnsureClientAsync(string email, string clientId, int tableNumber)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var organizationId = await db.UserSet.Where(u => u.Email == normalized).Select(u => u.OrganizationId).FirstOrDefaultAsync();
        if (organizationId is not int id)
        {
            logger.LogWarning("Development seed: {Email} has no organization, the client id was not applied.", normalized);
            return;
        }

        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == id);
        if (organization.ClientId != clientId)
        {
            if (await db.OrganizationSet.AnyAsync(o => o.ClientId == clientId && o.Id != id))
            {
                logger.LogWarning("Development seed: client id {ClientId} already belongs to another organization.", clientId);
                return;
            }

            organization.ClientId = clientId;
            await db.SaveChangesAsync();
        }

        await runner.RunAsync<DataContext>(id, async tenantDb =>
        {
            if (await tenantDb.BilliardTableSet.AnyAsync(t => t.ScoreboardNo == tableNumber && t.DeletedAt == null))
            {
                return;
            }

            var existing = await tenantDb.BilliardTableSet.FirstOrDefaultAsync(t => t.Number == tableNumber && t.DeletedAt == null);
            if (existing is null)
            {
                tenantDb.BilliardTableSet.Add(new BilliardTable
                {
                    OrganizationId = id,
                    Number = tableNumber,
                    Type = TableType.Match284,
                    Status = TableStatus.Available,
                    SortOrder = tableNumber,
                    ScoreboardNo = tableNumber
                });
            }
            else
            {
                existing.ScoreboardNo = tableNumber;
            }

            await tenantDb.SaveChangesAsync();
        });
    }
}
