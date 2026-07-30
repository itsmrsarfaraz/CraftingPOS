using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly AppDbContext _context;

    public ProductVariantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductVariant>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.VariantName)
            .ToListAsync();
    }

    public async Task<ProductVariant?> GetByIdAsync(int id)
    {
        return await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null)
    {
        var query = _context.ProductVariants.IgnoreQueryFilters().Where(v => v.Barcode == barcode);
        if (excludeId.HasValue) query = query.Where(v => v.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
    {
        var query = _context.ProductVariants.IgnoreQueryFilters().Where(v => v.SKU == sku);
        if (excludeId.HasValue) query = query.Where(v => v.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task AddAsync(ProductVariant variant)
    {
        await _context.ProductVariants.AddAsync(variant);
    }

    public Task UpdateAsync(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}