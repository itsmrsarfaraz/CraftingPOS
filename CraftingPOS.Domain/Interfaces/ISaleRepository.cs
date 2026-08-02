using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface ISaleRepository
{
    Task<List<Sale>> GetAllAsync();
    Task<Sale?> GetByIdAsync(int id);
    Task<List<Sale>> GetTodaysSalesAsync();
    Task<List<Sale>> GetByCustomerIdAsync(int customerId);
    Task<List<Sale>> GetRecentAsync(int count);
    Task<int> CountAsync();
    Task AddAsync(Sale sale);
    Task SaveChangesAsync();
}