namespace CraftingPOS.Application.DTOs;

public class ReceiptLineDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class ReceiptDto
{
    // TODO (Sprint 19 - Settings): source these from the Settings table
    // instead of hardcoded defaults, once business configuration exists.
    public string BusinessName { get; set; } = "CraftingPOS Store";
    public string? BusinessAddress { get; set; }
    public string? BusinessPhone { get; set; }
    public string FooterMessage { get; set; } = "Thank You For Shopping!";

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    public List<ReceiptLineDto> Items { get; set; } = new();

    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public decimal? AmountReceived { get; set; }
    public decimal? ChangeDue { get; set; }
}