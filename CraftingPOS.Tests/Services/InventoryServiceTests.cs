using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Services;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Persistence.Repositories;
using CraftingPOS.Tests.TestSupport;
using Xunit;

namespace CraftingPOS.Tests.Services;

public class InventoryServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        _fixture = new SqliteInMemoryFixture();

        var productRepository = new ProductRepository(_fixture.Context);
        var variantRepository = new ProductVariantRepository(_fixture.Context);
        var transactionRepository = new InventoryTransactionRepository(_fixture.Context);
        var currentUserContext = FakeCurrentUserContext.For(1, "owner1", RoleNames.Owner);

        _service = new InventoryService(
            productRepository, variantRepository, transactionRepository,
            currentUserContext, new FakeAuditLogService());
    }

    [Fact]
    public async Task AdjustStock_DecreaseBelowZero_IsRejected()
    {
        // BR-INV-001: Stock cannot become negative.
        var category = TestSeed.SeedCategory(_fixture.Context);
        var product = TestSeed.SeedProduct(_fixture.Context, category, stock: 5);

        var result = await _service.AdjustStockAsync(new AdjustStockDto
        {
            ProductId = product.Id,
            TransactionType = InventoryTransactionType.Damage,
            Quantity = 10, // more than the 5 in stock
            IsIncrease = false
        });

        Assert.False(result.Success);
        Assert.Contains("below zero", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Confirm stock was NOT modified despite the failed attempt.
        var reloaded = _fixture.Context.Products.First(p => p.Id == product.Id);
        Assert.Equal(5, reloaded.CurrentStock);
    }

    [Fact]
    public async Task AdjustStock_ValidDecrease_CreatesTransactionAndUpdatesStock()
    {
        // BR-INV-002: every stock change must generate a transaction record.
        var category = TestSeed.SeedCategory(_fixture.Context);
        var product = TestSeed.SeedProduct(_fixture.Context, category, stock: 20);

        var result = await _service.AdjustStockAsync(new AdjustStockDto
        {
            ProductId = product.Id,
            TransactionType = InventoryTransactionType.Damage,
            Quantity = 3,
            IsIncrease = false,
            Notes = "Broken in transit"
        });

        Assert.True(result.Success);

        var reloaded = _fixture.Context.Products.First(p => p.Id == product.Id);
        Assert.Equal(17, reloaded.CurrentStock);

        var transaction = _fixture.Context.InventoryTransactions.Single(t => t.ProductId == product.Id);
        Assert.Equal(InventoryTransactionType.Damage, transaction.TransactionType);
        Assert.Equal(-3, transaction.Quantity);
        Assert.Equal(20, transaction.StockBefore);
        Assert.Equal(17, transaction.StockAfter);
    }

    [Fact]
    public async Task AdjustStock_RejectsNonDamageOrAdjustmentTypes()
    {
        // Manual adjustments may only use Damage or Adjustment (Purchase/Sale/OpeningStock
        // are set exclusively by their own modules).
        var category = TestSeed.SeedCategory(_fixture.Context);
        var product = TestSeed.SeedProduct(_fixture.Context, category, stock: 20);

        var result = await _service.AdjustStockAsync(new AdjustStockDto
        {
            ProductId = product.Id,
            TransactionType = InventoryTransactionType.Sale,
            Quantity = 1,
            IsIncrease = true
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetLowStock_ReturnsOnlyItemsAtOrBelowMinimum()
    {
        var category = TestSeed.SeedCategory(_fixture.Context);
        TestSeed.SeedProduct(_fixture.Context, category, name: "Low Stock Item", barcode: "L1", sku: "SL1", stock: 2, minStock: 5);
        TestSeed.SeedProduct(_fixture.Context, category, name: "Healthy Stock Item", barcode: "L2", sku: "SL2", stock: 50, minStock: 5);

        var lowStock = await _service.GetLowStockAsync();

        Assert.Single(lowStock);
        Assert.Equal("Low Stock Item", lowStock[0].DisplayName);
    }

    public void Dispose() => _fixture.Dispose();
}