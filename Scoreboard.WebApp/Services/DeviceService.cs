using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>Scoreboard tablets / displays paired to a salon. The pairing code is what the kiosk sends as X-Client-Id.</summary>
public class DeviceService(DataContext db)
{
    public Task<List<Device>> ListAsync(int organizationId) =>
        db.DeviceSet
            .AsNoTracking()
            .Include(d => d.Table)
            .Where(d => d.OrganizationId == organizationId && d.DeletedAt == null)
            .OrderBy(d => d.Id)
            .ToListAsync();

    public async Task<Device> RegisterAsync(int organizationId, string? name, int? tableId, DeviceType type = DeviceType.Scoreboard)
    {
        if (tableId is not null)
        {
            var table = await db.BilliardTableSet.FirstOrDefaultAsync(t =>
                t.Id == tableId && t.OrganizationId == organizationId && t.DeletedAt == null)
                ?? throw new ArgumentException("Masa bulunamadı.");
            if (await db.DeviceSet.AnyAsync(d => d.TableId == table.Id && d.DeletedAt == null))
            {
                throw new ArgumentException($"{table.Number} numaralı masaya zaten bir cihaz bağlı.");
            }
        }

        string code;
        do
        {
            code = PairingCodeGenerator.Generate();
        } while (await db.DeviceSet.AnyAsync(d => d.PairingCode == code));

        var device = new Device
        {
            OrganizationId = organizationId,
            TableId = tableId,
            Type = type,
            Name = string.IsNullOrWhiteSpace(name) ? $"Cihaz {code}" : name.Trim(),
            PairingCode = code,
            PairedAt = DateTimeOffset.UtcNow
        };
        db.DeviceSet.Add(device);
        await db.SaveChangesAsync();
        return device;
    }

    public async Task<bool> DeleteAsync(int organizationId, int id)
    {
        var device = await db.DeviceSet.FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == organizationId && d.DeletedAt == null);
        if (device is null)
        {
            return false;
        }

        // Uploaded match stats reference the device, so it is retired and its code freed.
        device.DeletedAt = DateTimeOffset.UtcNow;
        device.PairingCode = null;
        device.TableId = null;
        await db.SaveChangesAsync();
        return true;
    }
}
