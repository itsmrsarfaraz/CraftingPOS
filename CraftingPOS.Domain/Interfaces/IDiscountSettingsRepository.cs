using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IDiscountSettingsRepository
{
    Task<DiscountSettings> GetOrCreateAsync();
    Task UpdateAsync(DiscountSettings settings);
    Task SaveChangesAsync();
}