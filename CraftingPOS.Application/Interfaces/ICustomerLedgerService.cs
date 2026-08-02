using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ICustomerLedgerService
{
    /// <summary>FR-KHATA-003: Calculate outstanding balance.</summary>
    Task<decimal> GetOutstandingBalanceAsync(int customerId);

    /// <summary>FR-KHATA-004: Display ledger history.</summary>
    Task<CustomerLedgerSummaryDto> GetLedgerAsync(int customerId);

    /// <summary>FR-KHATA-002: Record customer payments.</summary>
    Task<OperationResult> RecordPaymentAsync(RecordPaymentDto dto);

    /// <summary>
    /// FR-KHATA-001: Record credit sales.
    /// Not called anywhere yet — Sprint 10 (Sales) will call this directly
    /// from checkout when PaymentMethod = Credit. No changes needed here
    /// when that sprint arrives.
    /// </summary>
    Task RecordCreditSaleAsync(int customerId, int saleId, decimal amount, string? notes);
}