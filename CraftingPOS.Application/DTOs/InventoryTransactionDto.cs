using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Application.DTOs;

public class InventoryTransactionDto
{
    public InventoryTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal StockBefore { get; set; }
    public decimal StockAfter { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? CreatedBy { get; set; }
}

public class AdjustStockDto
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    /// <summary>Only Damage or Adjustment are valid here — Purchase/Sale/SaleReturn/OpeningStock are set by their own modules.</summary>
    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>Always a positive number; direction (add/remove) is chosen separately.</summary>
    public decimal Quantity { get; set; }

    public bool IsIncrease { get; set; } // true = add to stock, false = remove from stock

    public string? Notes { get; set; }
}