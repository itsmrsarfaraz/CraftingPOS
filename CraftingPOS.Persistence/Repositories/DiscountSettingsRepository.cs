using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class DiscountSettingsRepository : IDiscountSettingsRepository
{
    private readonly AppDbContext _context;

    public DiscountSettingsRepository(AppDbContext context) => _context = context;

    public async Task<DiscountSettings> GetOrCreateAsync()
    {
        var settings = await _context.DiscountSettings.FirstOrDefaultAsync();
        if (settings != null) return settings;

        settings = new DiscountSettings();
        await _context.DiscountSettings.AddAsync(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public Task UpdateAsync(DiscountSettings settings)
    {
        _context.DiscountSettings.Update(settings);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}