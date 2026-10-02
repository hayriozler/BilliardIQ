using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class PricingService(DataContext db)
{
    public Task<List<PricingRule>> ListAsync() =>
        db.PricingRuleSet
            .AsNoTracking()
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.IsDefault).ThenBy(r => r.Name)
            .ToListAsync();

    public async Task<PricingRule> UpsertAsync(int id, string name, decimal hourlyRate, IEnumerable<PricingTimeSlot> slots, bool isDefault)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Fiyat listesi adı gerekli.");
        if (hourlyRate < 0) throw new ArgumentException("Saatlik ücret negatif olamaz.");

        var slotList = slots.ToList();
        foreach (var slot in slotList)
        {
            if (slot.DaysOfWeek.Count == 0) throw new ArgumentException("Her saat aralığı için en az bir gün seçin.");
            if (slot.HourlyRate < 0) throw new ArgumentException("Saatlik ücret negatif olamaz.");
        }

        PricingRule? rule = null;
        if (id != 0)
        {
            rule = await db.PricingRuleSet.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
        }

        if (rule is null)
        {
            rule = new PricingRule { OrganizationId = db.CurrentOrganizationId };
            db.PricingRuleSet.Add(rule);
        }

        var hasOtherDefault = await db.PricingRuleSet.AnyAsync(r =>
            r.IsDefault && r.Id != rule.Id && r.DeletedAt == null);
        if (isDefault || !hasOtherDefault)
        {
            foreach (var other in await db.PricingRuleSet.Where(r => r.IsDefault && r.Id != rule.Id).ToListAsync())
            {
                other.IsDefault = false;
            }

            rule.IsDefault = true;
        }
        else
        {
            rule.IsDefault = false;
        }

        rule.Name = name;
        rule.DefaultHourlyRate = hourlyRate;
        rule.TimeSlots = slotList
            .Select(s => new PricingTimeSlot { DaysOfWeek = [.. s.DaysOfWeek], From = s.From, To = s.To, HourlyRate = s.HourlyRate })
            .ToList();
        await db.SaveChangesAsync();
        return rule;
    }

    public async Task DeleteAsync(int id)
    {
        var rule = await db.PricingRuleSet.FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null)
            ?? throw new InvalidOperationException("Fiyat listesi bulunamadı.");
        if (rule.IsDefault)
        {
            throw new InvalidOperationException("Varsayılan fiyat listesi silinemez. Önce başka bir listeyi varsayılan yapın.");
        }

        foreach (var table in await db.BilliardTableSet.Where(t => t.PricingRuleId == id).ToListAsync())
        {
            table.PricingRuleId = null;
        }

        rule.DeletedAt = DateTimeOffset.UtcNow;
        rule.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task UpdateBillingSettingsAsync(int roundingMinutes, int minimumMinutes)
    {
        if (roundingMinutes is < 1 or > 60) throw new ArgumentException("Yuvarlama 1-60 dakika arasında olmalı.");
        if (minimumMinutes is < 0 or > 600) throw new ArgumentException("Minimum süre 0-600 dakika arasında olmalı.");

        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == db.CurrentOrganizationId);
        organization.BillingRoundingMinutes = roundingMinutes;
        organization.MinimumBillableMinutes = minimumMinutes;
        await db.SaveChangesAsync();
    }
}
