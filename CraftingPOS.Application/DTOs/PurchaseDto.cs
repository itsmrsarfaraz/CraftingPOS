namespace CraftingPOS.Application.DTOs;

public class PurchaseDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? ProductVariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}

public class SavePurchaseDto
{
    public int SupplierId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
    public List<SavePurchaseItemDto> Items { get; set; } = new();
}

public class SavePurchaseItemDto
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}