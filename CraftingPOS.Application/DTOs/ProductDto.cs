namespace CraftingPOS.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string? BrandName { get; set; }

    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CraftingPOS.Domain.Enums.ProductType ProductType { get; set; }

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public string? ImagePath { get; set; }
    public string? ImageFullPath { get; set; }

    public bool IsActive { get; set; }
    public bool IsLowStock => CurrentStock <= MinimumStock;

    public CraftingPOS.Domain.Enums.DiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    public decimal EffectiveSellingPrice
    {
        get
        {
            if (DiscountType == CraftingPOS.Domain.Enums.DiscountType.Percentage)
                return Math.Max(0, SellingPrice - (SellingPrice * (DiscountValue ?? 0) / 100m));
            if (DiscountType == CraftingPOS.Domain.Enums.DiscountType.Flat)
                return Math.Max(0, SellingPrice - (DiscountValue ?? 0));
            return SellingPrice;
        }
    }
}

public class SaveProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CraftingPOS.Domain.Enums.ProductType ProductType { get; set; } = CraftingPOS.Domain.Enums.ProductType.Standard;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public string? NewImageSourcePath { get; set; }
    public bool AllowPriceOverride { get; set; }
}