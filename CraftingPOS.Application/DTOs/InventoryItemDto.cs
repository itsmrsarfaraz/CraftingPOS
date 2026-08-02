namespace CraftingPOS.Application.DTOs;

public enum StockStatus
{
    Normal,
    Low,
    OutOfStock
}

/// <summary>
/// One row per stock-tracking unit: a Standard product, or a single
/// variant of a Variable product (since stock lives on the variant,
/// not the parent, for Variable products — see Sprint 4).
/// </summary>
public class InventoryItemDto
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    public string DisplayName { get; set; } = string.Empty; // "Pepsi" or "T-Shirt — Small"
    public string CategoryName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public StockStatus Status =>
        CurrentStock <= 0 ? StockStatus.OutOfStock :
        CurrentStock <= MinimumStock ? StockStatus.Low :
        StockStatus.Normal;
}