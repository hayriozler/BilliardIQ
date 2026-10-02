using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class OrderService(DataContext db)
{
    public Task<List<OrderItem>> ListItemsAsync(int sessionId) =>
        db.OrderItemSet
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.Status != OrderItemStatus.Cancelled)
            .OrderBy(i => i.Id)
            .ToListAsync();

    public async Task AddItemAsync(int sessionId, int productId, int staffId)
    {
        var session = await FindOpenSessionAsync(sessionId);
        var product = await db.ProductSet.FirstOrDefaultAsync(p =>
            p.Id == productId && p.DeletedAt == null)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");
        if (!product.IsActive)
        {
            throw new InvalidOperationException("Bu ürün satışta değil.");
        }

        var item = await db.OrderItemSet.FirstOrDefaultAsync(i =>
            i.SessionId == sessionId && i.ProductId == productId && i.Status == OrderItemStatus.Ordered
            && i.UnitPriceSnapshot == product.Price);
        if (item is null)
        {
            item = new OrderItem
            {
                OrganizationId = db.CurrentOrganizationId,
                SessionId = sessionId,
                ProductId = product.Id,
                ProductNameSnapshot = product.Name,
                UnitPriceSnapshot = product.Price,
                VatRateSnapshot = product.VatRate,
                Status = OrderItemStatus.Ordered,
                OrderedByStaffId = staffId
            };
            db.OrderItemSet.Add(item);
        }

        item.Quantity += 1;
        item.LineTotal = Math.Round(item.Quantity * item.UnitPriceSnapshot - item.DiscountAmount, 2);
        await db.SaveChangesAsync();
        await RecalculateAsync(session);
    }

    public async Task RemoveOneAsync(int itemId)
    {
        var item = await db.OrderItemSet.FirstOrDefaultAsync(i =>
            i.Id == itemId && i.Status != OrderItemStatus.Cancelled)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");
        var session = await FindOpenSessionAsync(item.SessionId ?? 0);

        item.Quantity -= 1;
        if (item.Quantity <= 0)
        {
            item.Quantity = 0;
            item.Status = OrderItemStatus.Cancelled;
        }

        item.LineTotal = Math.Round(item.Quantity * item.UnitPriceSnapshot - item.DiscountAmount, 2);
        await db.SaveChangesAsync();
        await RecalculateAsync(session);
    }

    private async Task<TableSession> FindOpenSessionAsync(int sessionId)
    {
        var session = await db.TableSessionSet.FirstOrDefaultAsync(s => s.Id == sessionId)
            ?? throw new InvalidOperationException("Oturum bulunamadı.");
        if (session.Status is not (TableSessionStatus.Open or TableSessionStatus.Paused))
        {
            throw new InvalidOperationException("Oturum açık değil.");
        }

        return session;
    }

    private async Task RecalculateAsync(TableSession session)
    {
        session.ItemsAmount = await db.OrderItemSet
            .Where(i => i.SessionId == session.Id && i.Status != OrderItemStatus.Cancelled)
            .SumAsync(i => (decimal?)i.LineTotal) ?? 0;
        await db.SaveChangesAsync();
    }
}
