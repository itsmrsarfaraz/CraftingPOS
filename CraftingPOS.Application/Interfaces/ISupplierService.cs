using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllAsync();
    Task<OperationResult> SaveAsync(SaveSupplierDto dto);
    Task<OperationResult> DeactivateAsync(int id);
    Task<int> CountAsync();

    /// <summary>
    /// Returns purchase history for a supplier.
    /// TODO (Sprint 6 - Purchases): currently always returns an empty list
    /// since the Purchases table doesn't exist yet. Replace the body with a
    /// real query once Sprint 6 introduces IPurchaseRepository.
    /// </summary>
    Task<List<PurchaseHistoryItemDto>> GetPurchaseHistoryAsync(int supplierId);
}