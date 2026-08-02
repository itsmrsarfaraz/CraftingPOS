using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int CashierId { get; set; }
    public User Cashier { get; set; } = null!;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal SubTotal { get; set; }

    // Reserved for a future Discount Management sprint (Module 11 / ProductDiscounts).
    // Always 0 in V1 — populated automatically once product-level discounts exist.
    public decimal ProductDiscount { get; set; }

    public decimal CartDiscount { get; set; }

    // Reserved — no tax configuration exists yet (Settings sprint). Always 0 in V1.
    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public SalePaymentStatus PaymentStatus { get; set; }

    public string? Notes { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}