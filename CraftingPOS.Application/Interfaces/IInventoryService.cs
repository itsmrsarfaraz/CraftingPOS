using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IInventoryService
{
    /// <summary>FR-INV-001/002: all stock-tracking items with current levels.</summary>
    Task<List<InventoryItemDto>> GetAllAsync();

    /// <summary>FR-INV-003: low stock alerts (CurrentStock &lt;= MinimumStock), for the Inventory screen.</summary>
    Task<List<InventoryItemDto>> GetLowStockAsync();

    /// <summary>Dashboard feed — same query, capped and shaped for the summary card.</summary>
    Task<List<LowStockItemDto>> GetLowStockForDashboardAsync(int maxItems = 10);

    /// <summary>FR-INV-002: full transaction history for one product (Standard).</summary>
    Task<List<InventoryTransactionDto>> GetHistoryForProductAsync(int productId);

    /// <summary>FR-INV-002: full transaction history for one variant (Variable product).</summary>
    Task<List<InventoryTransactionDto>> GetHistoryForVariantAsync(int variantId);

    /// <summary>Manual stock adjustment (Damage or Adjustment types only). Enforces BR-INV-001.</summary>
    Task<OperationResult> AdjustStockAsync(AdjustStockDto dto);
}