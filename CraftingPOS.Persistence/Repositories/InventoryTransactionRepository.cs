using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;

namespace CraftingPOS.Persistence.Repositories;

public class InventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly AppDbContext _context;

    public InventoryTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InventoryTransaction transaction)
    {
        await _context.InventoryTransactions.AddAsync(transaction);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}