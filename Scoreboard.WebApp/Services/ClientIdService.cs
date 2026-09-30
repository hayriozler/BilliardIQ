using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>
/// The organization-wide client id that every table's scoreboard sends in X-Client-Id (together with X-Table-No).
/// </summary>
public class ClientIdService(DataContext db)
{
    /// <summary>Creates a client id that no organization uses yet.</summary>
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

    /// <summary>Replaces the client id. Every scoreboard must be reconfigured with the new value.</summary>
    public async Task<string> RegenerateAsync(int organizationId)
    {
        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == organizationId);
        organization.ClientId = await GenerateUniqueAsync();
        await db.SaveChangesAsync();
        return organization.ClientId;
    }

    /// <summary>Gives a client id to organizations created before client ids existed.</summary>
    public async Task EnsureAllAsync()
    {
        foreach (var organization in await db.OrganizationSet.Where(o => o.ClientId == null).ToListAsync())
        {
            organization.ClientId = await GenerateUniqueAsync();
            await db.SaveChangesAsync();
        }
    }
}
