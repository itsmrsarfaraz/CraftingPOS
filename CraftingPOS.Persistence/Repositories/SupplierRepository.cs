using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;

    public SupplierRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Supplier>> GetAllAsync()
    {
        return await _context.Suppliers.ToListAsync();
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        return await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.Suppliers.Where(s => s.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Suppliers.CountAsync();
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
    }

    public Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}