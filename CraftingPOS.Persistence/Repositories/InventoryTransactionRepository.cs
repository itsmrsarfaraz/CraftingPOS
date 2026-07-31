using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task<List<InventoryTransaction>> GetByProductIdAsync(int productId)
    {
        return await _context.InventoryTransactions
            .Where(t => t.ProductId == productId && t.ProductVariantId == null)
            .ToListAsync();
    }

    public async Task<List<InventoryTransaction>> GetByVariantIdAsync(int variantId)
    {
        return await _context.InventoryTransactions
            .Where(t => t.ProductVariantId == variantId)
            .ToListAsync();
    }
}