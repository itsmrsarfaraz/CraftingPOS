using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

public class Product : BaseEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Optional — not every business classifies by brand, so this stays nullable.
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ProductType ProductType { get; set; } = ProductType.Standard;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public string? ImagePath { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}