using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Sale> BaseQuery() =>
        _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Cashier)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Items).ThenInclude(i => i.ProductVariant)
            .Include(s => s.Payments);

    public async Task<List<Sale>> GetAllAsync()
    {
        return await BaseQuery().ToListAsync();
    }

    public async Task<List<Sale>> GetTodaysSalesAsync()
    {
        var todayUtcStart = DateTime.UtcNow.Date;
        var todayUtcEnd = todayUtcStart.AddDays(1);

        return await BaseQuery()
            .Where(s => s.SaleDate >= todayUtcStart && s.SaleDate < todayUtcEnd)
            .ToListAsync();
    }

    public async Task<List<Sale>> GetByCustomerIdAsync(int customerId)
    {
        return await BaseQuery()
            .Where(s => s.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<List<Sale>> GetRecentAsync(int count)
    {
        return await BaseQuery()
            .OrderByDescending(s => s.SaleDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Sales.CountAsync();
    }

    public async Task AddAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}