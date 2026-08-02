using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// Represents a single variant of a Variable Product (e.g. T-Shirt / Small).
/// Not used by Standard products.
/// </summary>
public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string VariantName { get; set; } = string.Empty; // e.g. "Small", "Red - Large"
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
}