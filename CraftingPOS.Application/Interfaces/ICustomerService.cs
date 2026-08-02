using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<List<CustomerDto>> SearchAsync(string searchTerm);
    Task<OperationResult> SaveAsync(SaveCustomerDto dto);
    Task<OperationResult> DeactivateAsync(int id);
    Task<int> CountAsync();

    /// <summary>
    /// TODO (Sprint 10 - Sales): currently always returns an empty list
    /// since the Sales table doesn't exist yet. Replace with a real query
    /// once Sprint 10 introduces ISalesRepository.
    /// </summary>
    Task<List<SalesHistoryItemDto>> GetSalesHistoryAsync(int customerId);
}