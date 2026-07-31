using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IInventoryTransactionRepository
{
    Task AddAsync(InventoryTransaction transaction);
    Task SaveChangesAsync();

    /// <summary>History for a Standard product (ProductVariantId is null).</summary>
    Task<List<InventoryTransaction>> GetByProductIdAsync(int productId);

    /// <summary>History for a specific variant of a Variable product.</summary>
    Task<List<InventoryTransaction>> GetByVariantIdAsync(int variantId);
}