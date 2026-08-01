using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IProductDiscountRepository
{
    Task<ProductDiscount?> GetByProductIdAsync(int productId);
    Task AddAsync(ProductDiscount discount);
    Task UpdateAsync(ProductDiscount discount);
    Task SaveChangesAsync();
}