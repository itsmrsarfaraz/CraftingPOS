using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Application.Services;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Persistence.Repositories;
using CraftingPOS.Tests.TestSupport;
using Moq;
using Xunit;

namespace CraftingPOS.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture;
    private readonly ProductRepository _productRepository;
    private readonly ProductDiscountRepository _discountRepository;
    private readonly Mock<IImageStorageService> _imageStorageMock = new();

    public ProductServiceTests()
    {
        _fixture = new SqliteInMemoryFixture();
        _productRepository = new ProductRepository(_fixture.Context);
        _discountRepository = new ProductDiscountRepository(_fixture.Context);
        TestSeed.SeedCategory(_fixture.Context);
    }

    private ProductService BuildService(string roleName) =>
    new(_productRepository, _discountRepository, _imageStorageMock.Object,
        FakeCurrentUserContext.For(1, "tester", roleName), new FakeAuditLogService());

    [Fact]
    public async Task Save_CashierSellingBelowCost_IsRejected()
    {
        // BR-PROD-003: Selling Price cannot be less than Cost Price.
        var service = BuildService(RoleNames.Cashier);
        var category = _fixture.Context.Categories.First();

        var result = await service.SaveAsync(new SaveProductDto
        {
            CategoryId = category.Id,
            Barcode = "B100",
            SKU = "S100",
            Name = "Test Item",
            CostPrice = 100,
            SellingPrice = 80, // below cost
            AllowPriceOverride = false
        });

        Assert.False(result.Success);
        Assert.Contains("cost price", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Save_OwnerSellingBelowCostWithOverride_Succeeds()
    {
        // BR-PROD-003 override allowed for Owner only.
        var service = BuildService(RoleNames.Owner);
        var category = _fixture.Context.Categories.First();

        var result = await service.SaveAsync(new SaveProductDto
        {
            CategoryId = category.Id,
            Barcode = "B101",
            SKU = "S101",
            Name = "Clearance Item",
            CostPrice = 100,
            SellingPrice = 80,
            AllowPriceOverride = true
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Save_DuplicateBarcode_IsRejected()
    {
        // BR-PROD-001: Barcode must be unique.
        var category = _fixture.Context.Categories.First();
        TestSeed.SeedProduct(_fixture.Context, category, barcode: "DUPLICATE");

        var service = BuildService(RoleNames.Owner);

        var result = await service.SaveAsync(new SaveProductDto
        {
            CategoryId = category.Id,
            Barcode = "DUPLICATE",
            SKU = "S999",
            Name = "New Item",
            CostPrice = 10,
            SellingPrice = 20
        });

        Assert.False(result.Success);
        Assert.Contains("already assigned", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deactivate_ThenReuseBarcode_IsStillRejected()
    {
        // BR-BAR-002: Deleted products shall not release their barcode for reuse.
        var category = _fixture.Context.Categories.First();
        var product = TestSeed.SeedProduct(_fixture.Context, category, barcode: "RETIRED");

        var service = BuildService(RoleNames.Owner);
        await service.DeactivateAsync(product.Id);

        var result = await service.SaveAsync(new SaveProductDto
        {
            CategoryId = category.Id,
            Barcode = "RETIRED",
            SKU = "SNEW",
            Name = "Reused Barcode Attempt",
            CostPrice = 10,
            SellingPrice = 20
        });

        Assert.False(result.Success);
    }

    public void Dispose() => _fixture.Dispose();
}