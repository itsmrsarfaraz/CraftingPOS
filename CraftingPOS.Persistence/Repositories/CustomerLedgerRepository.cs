using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class CustomerLedgerRepository : ICustomerLedgerRepository
{
    private readonly AppDbContext _context;

    public CustomerLedgerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerLedger>> GetByCustomerIdAsync(int customerId)
    {
        return await _context.CustomerLedgers
            .Where(l => l.CustomerId == customerId)
            .OrderBy(l => l.TransactionDate)
            .ToListAsync();
    }

    public async Task<CustomerLedger?> GetLatestEntryAsync(int customerId)
    {
        return await _context.CustomerLedgers
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.TransactionDate)
            .ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(CustomerLedger entry)
    {
        await _context.CustomerLedgers.AddAsync(entry);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}