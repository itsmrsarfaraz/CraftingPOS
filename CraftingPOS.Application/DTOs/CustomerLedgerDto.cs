namespace CraftingPOS.Application.DTOs;

public class CustomerLedgerEntryDto
{
    public DateTime TransactionDate { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
}

public class CustomerLedgerSummaryDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public List<CustomerLedgerEntryDto> Entries { get; set; } = new();
}

public class RecordPaymentDto
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}