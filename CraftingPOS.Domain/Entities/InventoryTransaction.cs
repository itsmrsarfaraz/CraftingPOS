using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// Immutable ledger of every stock change. Per SRS Part 5 §13:
/// "Never update stock silently. Every stock change must create InventoryTransaction."
/// Introduced in Sprint 6 (Purchases) so this rule holds from day one;
/// Sprint 9 builds the Inventory screen (history, alerts, manual adjustments)
/// on top of this same table.
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Null when the transaction applies to the parent Standard product;
    // set when it applies to a specific Variable product's variant.
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    public decimal Quantity { get; set; } // positive for increases, negative for decreases
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }

    // Points to the source record (e.g. PurchaseId, SaleId) depending on TransactionType.
    public int? ReferenceId { get; set; }

    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}