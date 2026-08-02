using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<List<Customer>> SearchAsync(string searchTerm);
    Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);
    Task<int> CountAsync();
    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task SaveChangesAsync();
}