using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Null when receiving a Standard product; set when receiving a specific variant.
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}