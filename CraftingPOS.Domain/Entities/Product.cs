using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

public class Product : BaseEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ProductType ProductType { get; set; } = ProductType.Standard;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    // For Standard products, stock is tracked here directly.
    // For Variable products, stock is tracked per-variant (see ProductVariant),
    // and CurrentStock/MinimumStock on the parent are kept at 0 / unused.
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public string? ImagePath { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}