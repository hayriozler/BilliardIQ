using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public record TableView(BilliardTable Table, TableSession? Session);

public record DashboardSummary(
    int TotalTables,
    int TablesInUse,
    int TablesAvailable,
    int TablesUnavailable,
    decimal RevenueToday,
    int SessionsToday,
    int MatchesToday);

public class TableService(DataContext db)
{
    public async Task<Organization> GetOrganizationAsync() =>
        await db.OrganizationSet.AsNoTracking().FirstAsync(o => o.Id == db.CurrentOrganizationId);

    public async Task<List<TableView>> ListAsync()
    {
        var tables = await db.BilliardTableSet
            .AsNoTracking()
            .Include(t => t.CurrentSession)
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Number)
            .ToListAsync();
        return tables.Select(t => new TableView(t, t.CurrentSession)).ToList();
    }

    public async Task<DashboardSummary> SummaryAsync()
    {
        var organization = await GetOrganizationAsync();
        var zone = PricingCalculator.ResolveTimeZone(organization.TimeZone);
        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
        var startOfDay = new DateTimeOffset(localNow.Date, localNow.Offset).ToUniversalTime();

        var statuses = await db.BilliardTableSet
            .Where(t => t.DeletedAt == null)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        int Count(params TableStatus[] wanted) => statuses.Where(s => wanted.Contains(s.Status)).Sum(s => s.Count);

        var sessionsToday = await db.TableSessionSet
            .Where(s => s.OpenedAt >= startOfDay && s.Status != TableSessionStatus.Voided)
            .Select(s => new { s.Status, s.TotalAmount })
            .ToListAsync();
        var revenue = sessionsToday
            .Where(s => s.Status is TableSessionStatus.Closed or TableSessionStatus.Settled)
            .Sum(s => s.TotalAmount ?? 0);

        var matches = await db.MatchesSet.CountAsync(m => m.CreatedAt >= startOfDay);

        return new DashboardSummary(
            statuses.Sum(s => s.Count),
            Count(TableStatus.InUse),
            Count(TableStatus.Available, TableStatus.Reserved),
            Count(TableStatus.Maintenance, TableStatus.OutOfService),
            revenue,
            sessionsToday.Count,
            matches);
    }

    public async Task<TableSession> OpenSessionAsync(int tableId, int staffId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var table = await LockTableAsync(tableId);
        if (table.Status is TableStatus.InUse || table.CurrentSessionId is not null)
        {
            throw new InvalidOperationException("Masa zaten açık.");
        }

        if (table.Status is TableStatus.Maintenance or TableStatus.OutOfService)
        {
            throw new InvalidOperationException("Masa şu an kullanıma kapalı.");
        }

        var rule = await ResolveRuleAsync(table.PricingRuleId);
        var session = new TableSession
        {
            OrganizationId = db.CurrentOrganizationId,
            TableId = table.Id,
            Status = TableSessionStatus.Open,
            OpenedAt = DateTimeOffset.UtcNow,
            OpenedByStaffId = staffId,
            PricingSnapshot = Snapshot(rule)
        };
        db.TableSessionSet.Add(session);
        await db.SaveChangesAsync();

        table.Status = TableStatus.InUse;
        table.CurrentSessionId = session.Id;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return session;
    }

    public Task PauseSessionAsync(int sessionId) => WithLockedSessionAsync(sessionId, async session =>
    {
        if (session.Status != TableSessionStatus.Open)
        {
            throw new InvalidOperationException("Oturum açık değil.");
        }

        session.Status = TableSessionStatus.Paused;
        session.PausedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return session;
    });

    public Task ResumeSessionAsync(int sessionId) => WithLockedSessionAsync(sessionId, async session =>
    {
        if (session.Status != TableSessionStatus.Paused || session.PausedAt is null)
        {
            throw new InvalidOperationException("Oturum duraklatılmamış.");
        }

        session.PausedMinutes += (int)Math.Round((DateTimeOffset.UtcNow - session.PausedAt.Value).TotalMinutes);
        session.PausedAt = null;
        session.Status = TableSessionStatus.Open;
        await db.SaveChangesAsync();
        return session;
    });

    public Task<TableSession> CloseSessionAsync(int sessionId, int staffId) => WithLockedSessionAsync(sessionId, async session =>
    {
        if (session.Status is not (TableSessionStatus.Open or TableSessionStatus.Paused))
        {
            throw new InvalidOperationException("Oturum zaten kapalı.");
        }

        var organization = await db.OrganizationSet.FirstAsync(o => o.Id == db.CurrentOrganizationId);
        var now = DateTimeOffset.UtcNow;
        var elapsed = PricingCalculator.ElapsedMinutes(session, now);
        var bill = PricingCalculator.Calculate(
            session.PricingSnapshot, session.OpenedAt, elapsed,
            organization.BillingRoundingMinutes, organization.MinimumBillableMinutes,
            PricingCalculator.ResolveTimeZone(organization.TimeZone));

        if (session.PausedAt is { } pausedAt)
        {
            session.PausedMinutes += (int)Math.Round((now - pausedAt).TotalMinutes);
            session.PausedAt = null;
        }

        session.ClosedAt = now;
        session.ClosedByStaffId = staffId;
        session.BilledMinutes = bill.BilledMinutes;
        session.TableAmount = bill.Amount;
        session.TotalAmount = Math.Max(0, bill.Amount + session.ItemsAmount - session.DiscountAmount);
        session.Status = TableSessionStatus.Closed;

        var table = await db.BilliardTableSet.FirstAsync(t => t.Id == session.TableId);
        table.Status = TableStatus.Available;
        table.CurrentSessionId = null;

        await db.SaveChangesAsync();
        return session;
    });

    public Task<TableSession> SettleSessionAsync(int sessionId, int staffId, PaymentMethod method) => WithLockedSessionAsync(sessionId, async session =>
    {
        if (session.Status != TableSessionStatus.Closed)
        {
            throw new InvalidOperationException("Sadece kapatılmış oturum tahsil edilebilir.");
        }

        var due = (session.TotalAmount ?? 0) - session.PaidAmount;
        if (due > 0)
        {
            db.PaymentSet.Add(new Payment
            {
                OrganizationId = db.CurrentOrganizationId,
                SessionId = session.Id,
                Amount = due,
                Method = method,
                Status = PaymentStatus.Completed,
                ReceivedByStaffId = staffId,
                PaidAt = DateTimeOffset.UtcNow
            });
            session.PaidAmount += due;
        }

        session.Status = TableSessionStatus.Settled;
        await db.SaveChangesAsync();
        return session;
    });

    public Task VoidPendingCollectionAsync(int sessionId, int staffId) => WithLockedSessionAsync(sessionId, async session =>
    {
        if (session.Status != TableSessionStatus.Closed)
        {
            throw new InvalidOperationException("Sadece bekleyen tahsilat silinebilir.");
        }

        session.Status = TableSessionStatus.Voided;
        session.VoidedAt = DateTimeOffset.UtcNow;
        session.VoidedByStaffId = staffId;
        await db.SaveChangesAsync();
        return session;
    });

    public Task<List<TableSession>> VoidedSessionsAsync(int take = 20) =>
        db.TableSessionSet
            .AsNoTracking()
            .Include(s => s.Table)
            .Where(s => s.Status == TableSessionStatus.Voided && s.VoidedAt != null)
            .OrderByDescending(s => s.VoidedAt)
            .Take(take)
            .ToListAsync();

    public Task<List<TableSession>> UnsettledSessionsAsync() =>
        db.TableSessionSet
            .AsNoTracking()
            .Include(s => s.Table)
            .Where(s => s.Status == TableSessionStatus.Closed)
            .OrderByDescending(s => s.ClosedAt)
            .ToListAsync();

    public Task<List<TableSession>> RecentSessionsAsync(int take = 15) =>
        db.TableSessionSet
            .AsNoTracking()
            .Include(s => s.Table)
            .Where(s => (s.Status == TableSessionStatus.Closed || s.Status == TableSessionStatus.Settled))
            .OrderByDescending(s => s.ClosedAt)
            .Take(take)
            .ToListAsync();

    public async Task<BilliardTable> UpsertTableAsync(int id, int number, string? label, TableType type, int? pricingRuleId, bool hasScoreboard = false)
    {
        if (number <= 0)
        {
            throw new ArgumentException("Masa numarası 1 veya daha büyük olmalı.");
        }

        if (await db.BilliardTableSet.AnyAsync(t =>
                t.Number == number && t.Id != id && t.DeletedAt == null))
        {
            throw new LocalizedArgumentException("{0} numaralı masa zaten var.", number);
        }

        if (pricingRuleId is not null &&
            !await db.PricingRuleSet.AnyAsync(r => r.Id == pricingRuleId))
        {
            throw new ArgumentException("Fiyat kuralı bulunamadı.");
        }

        BilliardTable table;
        if (id == 0)
        {
            table = new BilliardTable { OrganizationId = db.CurrentOrganizationId, Status = TableStatus.Available, SortOrder = number };
            db.BilliardTableSet.Add(table);
        }
        else
        {
            table = await FindTableAsync(id);
        }

        table.Number = number;
        table.Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        table.Type = type;
        table.PricingRuleId = pricingRuleId;

        var removedScoreboard = false;
        if (!hasScoreboard)
        {
            removedScoreboard = table.ScoreboardNo is not null;
            table.ScoreboardNo = null;
        }
        else if (table.ScoreboardNo is null)
        {
            table.ScoreboardNo = await NextScoreboardNoAsync(number);
        }

        await db.SaveChangesAsync();
        if (removedScoreboard)
        {
            await ForgetScoreboardAsync(table.Id);
        }
        return table;
    }

    private async Task<int> NextScoreboardNoAsync(int preferred)
    {
        var used = await db.BilliardTableSet
            .Where(t => t.DeletedAt == null && t.ScoreboardNo != null)
            .Select(t => t.ScoreboardNo!.Value)
            .ToListAsync();
        return used.Contains(preferred) ? used.Max() + 1 : preferred;
    }

    public async Task SetTableStatusAsync(int tableId, TableStatus status)
    {
        var table = await FindTableAsync(tableId);
        if (table.CurrentSessionId is not null)
        {
            throw new InvalidOperationException("Açık oturumu olan masanın durumu değiştirilemez. Önce oturumu kapatın.");
        }

        if (status == TableStatus.InUse)
        {
            throw new InvalidOperationException("Masa yalnızca oturum açılarak kullanıma alınır.");
        }

        table.Status = status;
        await db.SaveChangesAsync();
    }

    public async Task DeleteTableAsync(int tableId)
    {
        var table = await FindTableAsync(tableId);
        if (table.CurrentSessionId is not null)
        {
            throw new InvalidOperationException("Açık oturumu olan masa silinemez.");
        }

        table.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        await ForgetScoreboardAsync(table.Id);
    }

    private async Task ForgetScoreboardAsync(int tableId)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"EntityChange\" WHERE \"TableId\" = {tableId}");
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"ClientSync\" WHERE \"TableId\" = {tableId}");
    }

    private async Task<BilliardTable> FindTableAsync(int tableId) =>
        await db.BilliardTableSet.FirstOrDefaultAsync(t => t.Id == tableId && t.DeletedAt == null)
        ?? throw new InvalidOperationException("Masa bulunamadı.");

    private async Task<TableSession> FindSessionAsync(int sessionId) =>
        await db.TableSessionSet.FirstOrDefaultAsync(s => s.Id == sessionId)
        ?? throw new InvalidOperationException("Oturum bulunamadı.");

    private async Task<BilliardTable> LockTableAsync(int tableId) =>
        await db.BilliardTableSet
            .FromSql($"""SELECT * FROM "BilliardTable" WHERE "Id" = {tableId} FOR UPDATE""")
            .FirstOrDefaultAsync(t => t.DeletedAt == null)
        ?? throw new InvalidOperationException("Masa bulunamadı.");

    private async Task<T> WithLockedSessionAsync<T>(int sessionId, Func<TableSession, Task<T>> action)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var session = await db.TableSessionSet
            .FromSql($"""SELECT * FROM "TableSession" WHERE "Id" = {sessionId} FOR UPDATE""")
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Oturum bulunamadı.");
        var result = await action(session);
        await transaction.CommitAsync();
        return result;
    }

    private async Task<PricingRule> ResolveRuleAsync(int? preferredRuleId)
    {
        PricingRule? rule = null;
        if (preferredRuleId is not null)
        {
            rule = await db.PricingRuleSet.FirstOrDefaultAsync(r => r.Id == preferredRuleId && r.IsActive);
        }

        rule ??= await db.PricingRuleSet
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.IsDefault).ThenBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (rule is null)
        {
            rule = new PricingRule { OrganizationId = db.CurrentOrganizationId, Name = "Standart", IsDefault = true };
            db.PricingRuleSet.Add(rule);
            await db.SaveChangesAsync();
        }

        return rule;
    }

    private static PricingSnapshot Snapshot(PricingRule rule) => new()
    {
        PricingRuleId = rule.Id,
        Name = rule.Name,
        Currency = rule.Currency,
        DefaultHourlyRate = rule.DefaultHourlyRate,
        PerPlayerSurcharge = rule.PerPlayerSurcharge,
        MemberDiscountPercent = rule.MemberDiscountPercent,
        TimeSlots = rule.TimeSlots
            .Select(s => new PricingTimeSlot { DaysOfWeek = [.. s.DaysOfWeek], From = s.From, To = s.To, HourlyRate = s.HourlyRate })
            .ToList()
    };
}
