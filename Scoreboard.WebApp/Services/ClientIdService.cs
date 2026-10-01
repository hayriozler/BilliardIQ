using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class ClientIdService(DataContext db)
{
    public async Task<string> GenerateUniqueAsync()
    {
        string clientId;
        do
        {
            clientId = PairingCodeGenerator.Generate(10);
        } while (await db.OrganizationSet.AnyAsync(o => o.ClientId == clientId));

        return clientId;
    }

    public async Task<string> GetAsync(int organizationId)
    {
        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == organizationId);
        if (organization.ClientId is null)
        {
            organization.ClientId = await GenerateUniqueAsync();
            await db.SaveChangesAsync();
        }

        return organization.ClientId;
    }

    public async Task<string> RegenerateAsync(int organizationId)
    {
        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == organizationId);
        organization.ClientId = await GenerateUniqueAsync();
        await db.SaveChangesAsync();
        return organization.ClientId;
    }

    public async Task EnsureAllAsync()
    {
        foreach (var organization in await db.OrganizationSet.Where(o => o.ClientId == null).ToListAsync())
        {
            organization.ClientId = await GenerateUniqueAsync();
            await db.SaveChangesAsync();
        }
    }
}
