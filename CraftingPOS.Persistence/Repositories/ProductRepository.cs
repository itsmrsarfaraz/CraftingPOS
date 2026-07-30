using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();

        return await _context.Products
            .Include(p => p.Category)
            .Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Barcode.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term))
            .ToListAsync();
    }

    public async Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null)
    {
        // Intentionally checks ALL products regardless of IsActive (BR-BAR-002:
        // deleted products never release their barcode for reuse). The DbContext's
        // global query filter would hide inactive rows, so we bypass it here.
        var query = _context.Products.IgnoreQueryFilters().Where(p => p.Barcode == barcode);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> SkuExistsAsync(string sku, int? excludeId = null)
    {
        var query = _context.Products.IgnoreQueryFilters().Where(p => p.SKU == sku);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Products.CountAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}