using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IInventoryTransactionRepository
{
    Task AddAsync(InventoryTransaction transaction);
    Task SaveChangesAsync();
}