using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Cost captured AT TIME OF SALE, so profit reporting stays accurate
    // even if the product's CostPrice changes later.
    public decimal UnitCost { get; set; }

    // Reserved for future per-product discounts. Always 0 in V1.
    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}