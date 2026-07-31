using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IInventoryTransactionRepository _inventoryTransactionRepository;
    private readonly CurrentUserContext _currentUserContext;

    public PurchaseService(
        IPurchaseRepository purchaseRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        CurrentUserContext currentUserContext)
    {
        _purchaseRepository = purchaseRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _inventoryTransactionRepository = inventoryTransactionRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<PurchaseDto>> GetAllAsync()
    {
        var purchases = await _purchaseRepository.GetAllAsync();
        return purchases.OrderByDescending(p => p.PurchaseDate).Select(MapToDto).ToList();
    }

    public async Task<OperationResult<int>> SaveAsync(SavePurchaseDto dto)
    {
        // BR-SALE-style validation adapted for purchases:
        if (dto.SupplierId <= 0)
            return OperationResult<int>.Fail("Please select a supplier.");

        if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            return OperationResult<int>.Fail("Invoice number is required.");

        if (dto.Items == null || dto.Items.Count == 0)
            return OperationResult<int>.Fail("Add at least one item to the purchase.");

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
                return OperationResult<int>.Fail("Each item quantity must be greater than zero.");

            if (item.UnitCost < 0)
                return OperationResult<int>.Fail("Unit cost cannot be negative.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        var subTotal = dto.Items.Sum(i => i.Quantity * i.UnitCost);
        var totalAmount = subTotal - dto.DiscountAmount;

        var purchase = new Purchase
        {
            SupplierId = dto.SupplierId,
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            PurchaseDate = dto.PurchaseDate,
            SubTotal = subTotal,
            DiscountAmount = dto.DiscountAmount,
            TotalAmount = totalAmount,
            Notes = dto.Notes?.Trim(),
            CreatedBy = currentUsername
        };

        foreach (var item in dto.Items)
        {
            purchase.Items.Add(new PurchaseItem
            {
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                LineTotal = item.Quantity * item.UnitCost,
                CreatedBy = currentUsername
            });
        }

        await _purchaseRepository.AddAsync(purchase);
        await _purchaseRepository.SaveChangesAsync(); // assigns Purchase.Id and each PurchaseItem.Id

        // FR-PUR-003: Update Product Stock Automatically — and log an
        // InventoryTransaction for every single change (BR-INV-002).
        foreach (var item in purchase.Items)
        {
            if (item.ProductVariantId.HasValue)
            {
                var variant = await GetVariantOrThrowAsync(item.ProductVariantId.Value);
                var stockBefore = variant.CurrentStock;
                variant.CurrentStock += item.Quantity;

                await LogTransactionAsync(
                    productId: item.ProductId,
                    variantId: variant.Id,
                    type: InventoryTransactionType.Purchase,
                    quantity: item.Quantity,
                    stockBefore: stockBefore,
                    stockAfter: variant.CurrentStock,
                    referenceId: purchase.Id,
                    notes: $"Purchase Invoice: {purchase.InvoiceNumber}");
            }
            else
            {
                var product = await GetProductOrThrowAsync(item.ProductId);
                var stockBefore = product.CurrentStock;
                product.CurrentStock += item.Quantity;

                await LogTransactionAsync(
                    productId: product.Id,
                    variantId: null,
                    type: InventoryTransactionType.Purchase,
                    quantity: item.Quantity,
                    stockBefore: stockBefore,
                    stockAfter: product.CurrentStock,
                    referenceId: purchase.Id,
                    notes: $"Purchase Invoice: {purchase.InvoiceNumber}");
            }
        }

        await _productRepository.SaveChangesAsync();

        Log.Information("Purchase '{Invoice}' recorded for SupplierId {SupplierId} with {ItemCount} item(s) by '{User}'. Total: {Total}",
            purchase.InvoiceNumber, purchase.SupplierId, purchase.Items.Count, currentUsername, purchase.TotalAmount);

        return OperationResult<int>.Ok(purchase.Id);
    }

    private async Task<Product> GetProductOrThrowAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new InvalidOperationException($"Product {productId} not found while updating stock.");
        return product;
    }

    private async Task<ProductVariant> GetVariantOrThrowAsync(int variantId)
    {
        var variant = await _variantRepository.GetByIdAsync(variantId)
            ?? throw new InvalidOperationException($"Product variant {variantId} not found while updating stock.");
        return variant;
    }

    private async Task LogTransactionAsync(
        int productId, int? variantId, InventoryTransactionType type,
        decimal quantity, decimal stockBefore, decimal stockAfter, int referenceId, string? notes)
    {
        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            ProductVariantId = variantId,
            TransactionType = type,
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = _currentUserContext.Session?.Username ?? "system"
        };

        await _inventoryTransactionRepository.AddAsync(transaction);
    }

    private static PurchaseDto MapToDto(Purchase p)
    {
        return new PurchaseDto
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name ?? string.Empty,
            InvoiceNumber = p.InvoiceNumber,
            PurchaseDate = p.PurchaseDate,
            SubTotal = p.SubTotal,
            DiscountAmount = p.DiscountAmount,
            TotalAmount = p.TotalAmount,
            Notes = p.Notes,
            Items = p.Items.Select(i => new PurchaseItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductVariantId = i.ProductVariantId,
                VariantName = i.ProductVariant?.VariantName,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}