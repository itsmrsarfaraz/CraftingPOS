using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Application.DTOs;

/// <summary>Result of scanning/searching a product for the POS cart.</summary>
public class CartItemLookupDto
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal OriginalUnitPrice { get; set; }
    public decimal EffectiveUnitPrice { get; set; } // after any active product discount
    public decimal UnitCost { get; set; }
    public decimal AvailableStock { get; set; }
}

public class SaleDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal CartDiscount { get; set; }
    public decimal GrandTotal { get; set; }
    public SalePaymentStatus PaymentStatus { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
}

public class SaleItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class CompleteSaleDto
{
    public int? CustomerId { get; set; }
    public List<CompleteSaleItemDto> Items { get; set; } = new();

    public CraftingPOS.Domain.Enums.DiscountType CartDiscountType { get; set; } = CraftingPOS.Domain.Enums.DiscountType.Flat;
    public decimal CartDiscountValue { get; set; }

    /// <summary>True once an Owner/SystemAdmin has authorized exceeding the cashier ceiling for this transaction.</summary>
    public bool DiscountOverrideAuthorized { get; set; }

    /// <summary>True once an Owner has confirmed selling one or more items below cost.</summary>
    public bool BelowCostConfirmed { get; set; }

    public CraftingPOS.Domain.Enums.PaymentMethod PaymentMethod { get; set; }
    public decimal AmountReceived { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class CompleteSaleDto
{
    public int? CustomerId { get; set; }
    public List<CompleteSaleItemDto> Items { get; set; } = new();

    public CraftingPOS.Domain.Enums.DiscountType CartDiscountType { get; set; } = CraftingPOS.Domain.Enums.DiscountType.Flat;
    public decimal CartDiscountValue { get; set; }

    /// <summary>True once an Owner/SystemAdmin has authorized exceeding the cashier ceiling for this transaction.</summary>
    public bool DiscountOverrideAuthorized { get; set; }

    /// <summary>True once an Owner has confirmed selling one or more items below cost.</summary>
    public bool BelowCostConfirmed { get; set; }

    public CraftingPOS.Domain.Enums.PaymentMethod PaymentMethod { get; set; }
    public decimal AmountReceived { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class CompletedSaleResultDto
{
    public int SaleId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public decimal? ChangeDue { get; set; } // set only for Cash payments
}