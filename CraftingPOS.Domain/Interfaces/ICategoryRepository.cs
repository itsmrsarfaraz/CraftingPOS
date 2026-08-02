using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<Category?> GetByNameAsync(string name);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task SaveChangesAsync();
}