using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<List<Product>> SearchAsync(string searchTerm);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task<int> CountAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task SaveChangesAsync();
}