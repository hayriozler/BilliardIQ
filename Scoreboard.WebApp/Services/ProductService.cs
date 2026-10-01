using Microsoft.EntityFrameworkCore;
using Scoreboard.WebApp.Data;

namespace Scoreboard.WebApp.Services;

public class ProductService(DataContext db)
{
    public Task<List<ProductCategory>> ListCategoriesAsync(int organizationId) =>
        db.ProductCategorySet
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

    public Task<List<Product>> ListProductsAsync(int organizationId) =>
        db.ProductSet
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.DeletedAt == null)
            .OrderBy(p => p.Category.SortOrder).ThenBy(p => p.Category.Name).ThenBy(p => p.Name)
            .ToListAsync();

    public async Task<ProductCategory> UpsertCategoryAsync(int organizationId, int id, string name)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Kategori adı gerekli.");
        if (name.Length > 100) throw new ArgumentException("Kategori adı en fazla 100 karakter olabilir.");

        var category = id != 0
            ? await db.ProductCategorySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            : null;
        if (id != 0 && category is null) throw new ArgumentException("Kategori bulunamadı.");

        if (await db.ProductCategorySet.AnyAsync(c =>
                c.OrganizationId == organizationId && c.DeletedAt == null && c.Id != id && c.Name.ToLower() == name.ToLower()))
        {
            throw new ArgumentException("Bu isimde bir kategori zaten var.");
        }

        if (category is null)
        {
            var next = await db.ProductCategorySet.Where(c => c.OrganizationId == organizationId).MaxAsync(c => (int?)c.SortOrder) ?? 0;
            category = new ProductCategory { OrganizationId = organizationId, SortOrder = next + 1 };
            db.ProductCategorySet.Add(category);
        }

        category.Name = name;
        await db.SaveChangesAsync();
        return category;
    }

    public async Task MoveCategoryAsync(int organizationId, int id, int direction)
    {
        var list = await db.ProductCategorySet
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();
        var index = list.FindIndex(c => c.Id == id);
        if (index < 0) throw new InvalidOperationException("Kategori bulunamadı.");

        var target = index + Math.Sign(direction);
        if (target >= 0 && target < list.Count)
        {
            (list[index], list[target]) = (list[target], list[index]);
        }

        for (var i = 0; i < list.Count; i++)
        {
            list[i].SortOrder = i + 1;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int organizationId, int id)
    {
        var category = await db.ProductCategorySet.FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == organizationId && c.DeletedAt == null)
            ?? throw new InvalidOperationException("Kategori bulunamadı.");
        if (await db.ProductSet.AnyAsync(p => p.CategoryId == id && p.DeletedAt == null))
        {
            throw new InvalidOperationException("Ürünü olan kategori silinemez. Önce ürünleri silin veya taşıyın.");
        }

        category.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<Product> UpsertProductAsync(
        int organizationId, int id, int categoryId, string name, decimal price, decimal vatRate, bool isActive)
    {
        name = name.Trim();
        if (name.Length == 0) throw new ArgumentException("Ürün adı gerekli.");
        if (name.Length > 150) throw new ArgumentException("Ürün adı en fazla 150 karakter olabilir.");
        if (price < 0) throw new ArgumentException("Fiyat negatif olamaz.");
        if (vatRate is < 0 or > 100) throw new ArgumentException("KDV oranı 0-100 arasında olmalı.");

        if (!await db.ProductCategorySet.AnyAsync(c => c.Id == categoryId && c.OrganizationId == organizationId && c.DeletedAt == null))
        {
            throw new ArgumentException("Kategori seçin.");
        }

        var product = id != 0
            ? await db.ProductSet.FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId && p.DeletedAt == null)
            : null;
        if (id != 0 && product is null) throw new ArgumentException("Ürün bulunamadı.");

        if (product is null)
        {
            product = new Product { OrganizationId = organizationId };
            db.ProductSet.Add(product);
        }

        product.CategoryId = categoryId;
        product.Name = name;
        product.Price = Math.Round(price, 2);
        product.VatRate = Math.Round(vatRate, 2);
        product.IsActive = isActive;
        await db.SaveChangesAsync();
        return product;
    }

    public async Task DeleteProductAsync(int organizationId, int id)
    {
        var product = await db.ProductSet.FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId && p.DeletedAt == null)
            ?? throw new InvalidOperationException("Ürün bulunamadı.");
        product.DeletedAt = DateTimeOffset.UtcNow;
        product.IsActive = false;
        await db.SaveChangesAsync();
    }
}
