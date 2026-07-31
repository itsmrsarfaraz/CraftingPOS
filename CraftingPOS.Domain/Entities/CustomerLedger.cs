using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// Append-only ledger of customer credit transactions (Khata).
/// BR-KHATA-001: Outstanding balance shall never be manually edited —
/// enforced by only ever inserting new rows, never updating existing ones.
/// BR-KHATA-002: Balance calculated from transactions only — each row
/// stores the running balance immediately after that transaction.
/// </summary>
public class CustomerLedger : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Set by Sales (Sprint 10) for credit-sale entries. No FK yet since the
    // Sales table doesn't exist until Sprint 10 — plain reference for now.
    public int? SaleId { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    public decimal Debit { get; set; }   // increases balance owed (credit sale)
    public decimal Credit { get; set; }  // decreases balance owed (customer payment)
    public decimal Balance { get; set; } // running balance snapshot after this entry

    public string? Notes { get; set; }
}