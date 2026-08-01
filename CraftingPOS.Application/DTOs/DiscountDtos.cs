using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Application.DTOs;

public class ProductDiscountDto
{
    public int ProductId { get; set; }
    public DiscountType? DiscountType { get; set; } // null = no active discount
    public decimal? DiscountValue { get; set; }
}

public class SaveProductDiscountDto
{
    public int ProductId { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
}

public class DiscountSettingsDto
{
    public decimal MaxCashierDiscountPercent { get; set; }
    public decimal MaxCashierDiscountFlat { get; set; }
}