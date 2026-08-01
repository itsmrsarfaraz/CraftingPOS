using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// One active discount per product (FR-DISC-001/002). Simplified from the
/// SRS's date-ranged design — a single row per product is upserted rather
/// than kept as history, since V1 has no need to schedule future discounts.
/// </summary>
public class ProductDiscount : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
}