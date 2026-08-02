using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Services;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Persistence.Repositories;
using CraftingPOS.Tests.TestSupport;
using Xunit;

namespace CraftingPOS.Tests.Services;

public class CustomerLedgerServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture;
    private readonly CustomerLedgerService _service;
    private readonly int _customerId;

    public CustomerLedgerServiceTests()
    {
        _fixture = new SqliteInMemoryFixture();

        var customer = new Domain.Entities.Customer { Name = "Test Customer" };
        _fixture.Context.Customers.Add(customer);
        _fixture.Context.SaveChanges();
        _customerId = customer.Id;

        var ledgerRepository = new CustomerLedgerRepository(_fixture.Context);
        var customerRepository = new CustomerRepository(_fixture.Context);
        var currentUserContext = FakeCurrentUserContext.For(1, "owner1", RoleNames.Owner);

        _service = new CustomerLedgerService(ledgerRepository, customerRepository, currentUserContext);
    }

    [Fact]
    public async Task RecordCreditSale_ThenPayment_ProducesCorrectRunningBalance()
    {
        // BR-KHATA-002: balance calculated from transactions only.
        await _service.RecordCreditSaleAsync(_customerId, saleId: 1, amount: 1000, notes: "Credit sale");
        var balanceAfterSale = await _service.GetOutstandingBalanceAsync(_customerId);
        Assert.Equal(1000, balanceAfterSale);

        await _service.RecordPaymentAsync(new RecordPaymentDto { CustomerId = _customerId, Amount = 400 });
        var balanceAfterPayment = await _service.GetOutstandingBalanceAsync(_customerId);
        Assert.Equal(600, balanceAfterPayment);
    }

    [Fact]
    public async Task Ledger_IsAppendOnly_NeverOverwritesPreviousEntries()
    {
        // BR-KHATA-001: outstanding balance shall never be manually edited.
        await _service.RecordCreditSaleAsync(_customerId, saleId: 1, amount: 500, notes: null);
        await _service.RecordPaymentAsync(new RecordPaymentDto { CustomerId = _customerId, Amount = 200 });
        await _service.RecordCreditSaleAsync(_customerId, saleId: 2, amount: 300, notes: null);

        var ledger = await _service.GetLedgerAsync(_customerId);

        // 3 append-only entries must exist, in chronological order, each with its own snapshot balance.
        Assert.Equal(3, ledger.Entries.Count);
        Assert.Equal(500, ledger.Entries[0].Balance);
        Assert.Equal(300, ledger.Entries[1].Balance);
        Assert.Equal(600, ledger.Entries[2].Balance);
        Assert.Equal(600, ledger.OutstandingBalance);
    }

    [Fact]
    public async Task RecordPayment_WithZeroOrNegativeAmount_IsRejected()
    {
        var result = await _service.RecordPaymentAsync(new RecordPaymentDto { CustomerId = _customerId, Amount = 0 });
        Assert.False(result.Success);

        var resultNegative = await _service.RecordPaymentAsync(new RecordPaymentDto { CustomerId = _customerId, Amount = -50 });
        Assert.False(resultNegative.Success);
    }

    public void Dispose() => _fixture.Dispose();
}