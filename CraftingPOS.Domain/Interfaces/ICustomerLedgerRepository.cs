using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface ICustomerLedgerRepository
{
    Task<List<CustomerLedger>> GetByCustomerIdAsync(int customerId);
    Task<CustomerLedger?> GetLatestEntryAsync(int customerId);
    Task AddAsync(CustomerLedger entry);
    Task SaveChangesAsync();
}