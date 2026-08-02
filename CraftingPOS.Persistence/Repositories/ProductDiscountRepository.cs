using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class ProductDiscountRepository : IProductDiscountRepository
{
    private readonly AppDbContext _context;

    public ProductDiscountRepository(AppDbContext context) => _context = context;

    public async Task<ProductDiscount?> GetByProductIdAsync(int productId)
    {
        return await _context.ProductDiscounts.FirstOrDefaultAsync(d => d.ProductId == productId);
    }

    public async Task AddAsync(ProductDiscount discount) => await _context.ProductDiscounts.AddAsync(discount);

    public Task UpdateAsync(ProductDiscount discount)
    {
        _context.ProductDiscounts.Update(discount);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}