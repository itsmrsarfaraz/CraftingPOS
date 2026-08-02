namespace CraftingPOS.Application.DTOs;

/// <summary>
/// Shape for a supplier's purchase history row.
/// Populated with real data starting Sprint 6 (Purchases module).
/// Defined now so the Suppliers screen never needs restructuring later.
/// </summary>
public class PurchaseHistoryItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
}