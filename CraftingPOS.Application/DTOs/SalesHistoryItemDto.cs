namespace CraftingPOS.Application.DTOs;

/// <summary>
/// Shape for a customer's sales/purchase history row.
/// Populated with real data starting Sprint 10 (Sales & Billing).
/// Defined now so the Customers screen never needs restructuring later.
/// </summary>
public class SalesHistoryItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}