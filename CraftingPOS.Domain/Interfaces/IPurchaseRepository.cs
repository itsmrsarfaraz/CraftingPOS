using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IPurchaseRepository
{
    Task<List<Purchase>> GetAllAsync();
    Task<Purchase?> GetByIdAsync(int id);
    Task<List<Purchase>> GetBySupplierIdAsync(int supplierId);
    Task AddAsync(Purchase purchase);
    Task SaveChangesAsync();
}