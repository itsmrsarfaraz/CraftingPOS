using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Services;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Persistence.Repositories;
using CraftingPOS.Tests.TestSupport;
using Xunit;

namespace CraftingPOS.Tests.Services;

public class SaleServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture;
    private readonly Product _product;
    private readonly User _cashierUser;
    private readonly User _ownerUser;

    public SaleServiceTests()
    {
        _fixture = new SqliteInMemoryFixture();

        var category = TestSeed.SeedCategory(_fixture.Context);
        _product = TestSeed.SeedProduct(_fixture.Context, category, costPrice: 100, sellingPrice: 150, stock: 10);

        // Sale.CashierId is a real FK to Users, so tests that actually
        // complete a sale need a genuine seeded User row, not just a
        // fabricated in-memory session.
        var (ownerRole, cashierRole) = TestSeed.SeedRoles(_fixture.Context);
        _ownerUser = TestSeed.SeedUser(_fixture.Context, ownerRole, "owner1", "hash");
        _cashierUser = TestSeed.SeedUser(_fixture.Context, cashierRole, "cashier1", "hash");

        _fixture.Context.DiscountSettings.Add(new DiscountSettings
        {
            MaxCashierDiscountPercent = 5,
            MaxCashierDiscountFlat = 200
        });
        _fixture.Context.SaveChanges();
    }

    private SaleService BuildService(string roleName)
    {
        var saleRepository = new SaleRepository(_fixture.Context);
        var productRepository = new ProductRepository(_fixture.Context);
        var variantRepository = new ProductVariantRepository(_fixture.Context);
        var discountRepository = new ProductDiscountRepository(_fixture.Context);
        var discountSettingsRepository = new DiscountSettingsRepository(_fixture.Context);
        var transactionRepository = new InventoryTransactionRepository(_fixture.Context);

        // Use the real seeded user's Id so Sale.CashierId's FK constraint is satisfied.
        var user = roleName == RoleNames.Owner ? _ownerUser : _cashierUser;
        var currentUserContext = FakeCurrentUserContext.For(user.Id, user.Username, roleName);

        var customerLedgerService = new CustomerLedgerService(
            new CustomerLedgerRepository(_fixture.Context),
            new CustomerRepository(_fixture.Context),
            currentUserContext);

        return new SaleService(
            saleRepository, productRepository, variantRepository,
            discountRepository, discountSettingsRepository, transactionRepository,
            customerLedgerService, new FakeAuditLogService(), currentUserContext);
    }

    [Fact]
    public async Task CompleteSale_WithEmptyCart_IsRejected()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>(),
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 0
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CompleteSale_ExceedingAvailableStock_IsRejected()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 999, UnitPrice = 150, UnitCost = 100 }
            },
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 999 * 150
        });

        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteSale_ValidCashSale_DeductsStockAndReturnsCorrectChange()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 2, UnitPrice = 150, UnitCost = 100 }
            },
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 400
        });

        Assert.True(result.Success);
        Assert.Equal(100, result.Data!.ChangeDue);

        var reloaded = _fixture.Context.Products.First(p => p.Id == _product.Id);
        Assert.Equal(8, reloaded.CurrentStock);

        var transaction = _fixture.Context.InventoryTransactions.Single(t => t.ProductId == _product.Id);
        Assert.Equal(InventoryTransactionType.Sale, transaction.TransactionType);
        Assert.Equal(-2, transaction.Quantity);
    }

    [Fact]
    public async Task CompleteSale_CashierBelowCost_IsHardRejected()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 1, UnitPrice = 90, UnitCost = 100 }
            },
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 90,
            BelowCostConfirmed = false
        });

        Assert.False(result.Success);
        Assert.Contains("cannot sell", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteSale_OwnerBelowCostWithoutConfirmation_RequestsConfirmation()
    {
        var service = BuildService(RoleNames.Owner);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 1, UnitPrice = 90, UnitCost = 100 }
            },
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 90,
            BelowCostConfirmed = false
        });

        Assert.False(result.Success);
        Assert.StartsWith("OWNER_CONFIRM_BELOW_COST", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteSale_OwnerBelowCostWithConfirmation_Succeeds()
    {
        var service = BuildService(RoleNames.Owner);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 1, UnitPrice = 90, UnitCost = 100 }
            },
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 90,
            BelowCostConfirmed = true
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CompleteSale_CashierDiscountBeyondCeiling_RequiresOwnerAuth()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 10, UnitPrice = 150, UnitCost = 100 }
            },
            CartDiscountType = DiscountType.Flat,
            CartDiscountValue = 300,
            DiscountOverrideAuthorized = false,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 1200
        });

        Assert.False(result.Success);
        Assert.StartsWith("OWNER_AUTH_REQUIRED", result.ErrorMessage);
    }

    [Fact]
    public async Task CompleteSale_CashierDiscountWithinCeiling_Succeeds()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 10, UnitPrice = 150, UnitCost = 100 }
            },
            CartDiscountType = DiscountType.Percentage,
            CartDiscountValue = 3,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 1500
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CompleteSale_CashierDiscountBeyondCeiling_WithAuthorization_Succeeds()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 10, UnitPrice = 150, UnitCost = 100 }
            },
            CartDiscountType = DiscountType.Flat,
            CartDiscountValue = 300,
            DiscountOverrideAuthorized = true,
            PaymentMethod = PaymentMethod.Cash,
            AmountReceived = 1200
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CompleteSale_CreditWithoutCustomer_IsRejected()
    {
        var service = BuildService(RoleNames.Cashier);

        var result = await service.CompleteSaleAsync(new CompleteSaleDto
        {
            Items = new List<CompleteSaleItemDto>
            {
                new() { ProductId = _product.Id, Quantity = 1, UnitPrice = 150, UnitCost = 100 }
            },
            CustomerId = null,
            PaymentMethod = PaymentMethod.Credit
        });

        Assert.False(result.Success);
    }

    public void Dispose() => _fixture.Dispose();
}