using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

/// <summary>Products and services added to an open table's bill. The session keeps a denormalized ItemsAmount that the close/settle steps use.</summary>
public class OrderService(DataContext db)
{
    public Task<List<OrderItem>> ListItemsAsync(int organizationId, int sessionId) =>
        db.OrderItemSet
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId && i.SessionId == sessionId && i.Status != OrderItemStatus.Cancelled)
            .OrderBy(i => i.Id)
            .ToListAsync();

    /// <summary>Adds one unit of a product to the session's bill (or one more of a line that already exists).</summary>
    public async Task AddItemAsync(int organizationId, int sessionId, int productId, int staffId)
    {
        var session = await FindOpenSessionAsync(organizationId, sessionId);
        var product = await db.ProductSet.FirstOrDefaultAsync(p =>
            p.Id == productId && p.OrganizationId == organizationId && p.DeletedAt == null)
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
                OrganizationId = organizationId,
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

    /// <summary>Takes one unit off a line; the line is cancelled when nothing is left.</summary>
    public async Task RemoveOneAsync(int organizationId, int itemId)
    {
        var item = await db.OrderItemSet.FirstOrDefaultAsync(i =>
            i.Id == itemId && i.OrganizationId == organizationId && i.Status != OrderItemStatus.Cancelled)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");
        var session = await FindOpenSessionAsync(organizationId, item.SessionId ?? 0);

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

    private async Task<TableSession> FindOpenSessionAsync(int organizationId, int sessionId)
    {
        var session = await db.TableSessionSet.FirstOrDefaultAsync(s => s.Id == sessionId && s.OrganizationId == organizationId)
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
