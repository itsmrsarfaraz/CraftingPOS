using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IProductVariantRepository
{
    Task<List<ProductVariant>> GetByProductIdAsync(int productId);
    Task<ProductVariant?> GetByIdAsync(int id);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null);
    Task<bool> SkuExistsAsync(string sku, int? excludeId = null);
    Task AddAsync(ProductVariant variant);
    Task UpdateAsync(ProductVariant variant);
    Task SaveChangesAsync();
}