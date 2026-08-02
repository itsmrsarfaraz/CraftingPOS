using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class CustomerLedgerService : ICustomerLedgerService
{
    private readonly ICustomerLedgerRepository _ledgerRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly CurrentUserContext _currentUserContext;

    public CustomerLedgerService(
        ICustomerLedgerRepository ledgerRepository,
        ICustomerRepository customerRepository,
        CurrentUserContext currentUserContext)
    {
        _ledgerRepository = ledgerRepository;
        _customerRepository = customerRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<decimal> GetOutstandingBalanceAsync(int customerId)
    {
        var latest = await _ledgerRepository.GetLatestEntryAsync(customerId);
        return latest?.Balance ?? 0m;
    }

    public async Task<CustomerLedgerSummaryDto> GetLedgerAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        var entries = await _ledgerRepository.GetByCustomerIdAsync(customerId);

        return new CustomerLedgerSummaryDto
        {
            CustomerId = customerId,
            CustomerName = customer?.Name ?? string.Empty,
            OutstandingBalance = entries.Count > 0 ? entries[^1].Balance : 0m,
            Entries = entries
                .OrderBy(e => e.TransactionDate)
                .Select(e => new CustomerLedgerEntryDto
                {
                    TransactionDate = e.TransactionDate,
                    Debit = e.Debit,
                    Credit = e.Credit,
                    Balance = e.Balance,
                    Notes = e.Notes
                })
                .ToList()
        };
    }

    public async Task<OperationResult> RecordPaymentAsync(RecordPaymentDto dto)
    {
        if (dto.Amount <= 0)
        {
            return OperationResult.Fail("Payment amount must be greater than zero.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";
        var currentBalance = await GetOutstandingBalanceAsync(dto.CustomerId);
        var newBalance = currentBalance - dto.Amount;

        var entry = new CustomerLedger
        {
            CustomerId = dto.CustomerId,
            SaleId = null,
            TransactionDate = DateTime.UtcNow,
            Debit = 0,
            Credit = dto.Amount,
            Balance = newBalance,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? "Payment received" : dto.Notes.Trim(),
            CreatedBy = currentUsername
        };

        await _ledgerRepository.AddAsync(entry);
        await _ledgerRepository.SaveChangesAsync();

        Log.Information("Payment of {Amount} recorded for CustomerId {CustomerId} by '{User}'. New balance: {Balance}",
            dto.Amount, dto.CustomerId, currentUsername, newBalance);

        return OperationResult.Ok();
    }

    public async Task RecordCreditSaleAsync(int customerId, int saleId, decimal amount, string? notes)
    {
        // BR-KHATA-002: balance calculated from transactions only.
        var currentBalance = await GetOutstandingBalanceAsync(customerId);
        var newBalance = currentBalance + amount;

        var entry = new CustomerLedger
        {
            CustomerId = customerId,
            SaleId = saleId,
            TransactionDate = DateTime.UtcNow,
            Debit = amount,
            Credit = 0,
            Balance = newBalance,
            Notes = string.IsNullOrWhiteSpace(notes) ? $"Credit sale (Sale #{saleId})" : notes,
            CreatedBy = _currentUserContext.Session?.Username ?? "system"
        };

        await _ledgerRepository.AddAsync(entry);
        await _ledgerRepository.SaveChangesAsync();

        Log.Information("Credit sale of {Amount} recorded for CustomerId {CustomerId} (SaleId {SaleId}). New balance: {Balance}",
            amount, customerId, saleId, newBalance);
    }
}