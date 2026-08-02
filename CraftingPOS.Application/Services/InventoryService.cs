using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly CurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public InventoryService(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IInventoryTransactionRepository transactionRepository,
        CurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _transactionRepository = transactionRepository;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<List<InventoryItemDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var items = new List<InventoryItemDto>();

        foreach (var product in products)
        {
            if (product.ProductType == ProductType.Standard)
            {
                items.Add(MapProductToDto(product));
            }
            else
            {
                foreach (var variant in product.Variants.Where(v => v.IsActive))
                {
                    items.Add(MapVariantToDto(product, variant));
                }
            }
        }

        return items.OrderBy(i => i.DisplayName).ToList();
    }

    public async Task<List<InventoryItemDto>> GetLowStockAsync()
    {
        var all = await GetAllAsync();
        return all.Where(i => i.Status != StockStatus.Normal).ToList();
    }

    public async Task<List<LowStockItemDto>> GetLowStockForDashboardAsync(int maxItems = 10)
    {
        var lowStock = await GetLowStockAsync();

        return lowStock
            .OrderBy(i => i.CurrentStock)
            .Take(maxItems)
            .Select(i => new LowStockItemDto
            {
                ProductName = i.DisplayName,
                CurrentStock = i.CurrentStock,
                MinimumStock = i.MinimumStock
            })
            .ToList();
    }

    public async Task<List<InventoryTransactionDto>> GetHistoryForProductAsync(int productId)
    {
        var transactions = await _transactionRepository.GetByProductIdAsync(productId);
        return transactions.OrderByDescending(t => t.TransactionDate).Select(MapTransactionToDto).ToList();
    }

    public async Task<List<InventoryTransactionDto>> GetHistoryForVariantAsync(int variantId)
    {
        var transactions = await _transactionRepository.GetByVariantIdAsync(variantId);
        return transactions.OrderByDescending(t => t.TransactionDate).Select(MapTransactionToDto).ToList();
    }

    public async Task<OperationResult> AdjustStockAsync(AdjustStockDto dto)
    {
        if (dto.TransactionType != InventoryTransactionType.Damage &&
            dto.TransactionType != InventoryTransactionType.Adjustment)
        {
            return OperationResult.Fail("Manual stock changes may only use Damage or Adjustment types.");
        }

        if (dto.Quantity <= 0)
        {
            return OperationResult.Fail("Quantity must be greater than zero.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";
        var signedQuantity = dto.IsIncrease ? dto.Quantity : -dto.Quantity;

        if (dto.ProductVariantId.HasValue)
        {
            var variant = await _variantRepository.GetByIdAsync(dto.ProductVariantId.Value);
            if (variant == null)
                return OperationResult.Fail("Product variant not found.");

            var stockBefore = variant.CurrentStock;
            var stockAfter = stockBefore + signedQuantity;

            if (stockAfter < 0)
            {
                return OperationResult.Fail(
                    $"This would reduce stock below zero (current: {stockBefore}, requested reduction: {dto.Quantity}). Adjust the quantity.");
            }

            variant.CurrentStock = stockAfter;
            variant.UpdatedBy = currentUsername;
            await _variantRepository.UpdateAsync(variant);
            await _variantRepository.SaveChangesAsync();

            await LogTransactionAsync(variant.ProductId, variant.Id, dto.TransactionType, signedQuantity, stockBefore, stockAfter, dto.Notes, currentUsername);
        }
        else
        {
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null)
                return OperationResult.Fail("Product not found.");

            var stockBefore = product.CurrentStock;
            var stockAfter = stockBefore + signedQuantity;

            if (stockAfter < 0)
            {
                return OperationResult.Fail(
                    $"This would reduce stock below zero (current: {stockBefore}, requested reduction: {dto.Quantity}). Adjust the quantity.");
            }

            product.CurrentStock = stockAfter;
            product.UpdatedBy = currentUsername;
            await _productRepository.UpdateAsync(product);
            await _productRepository.SaveChangesAsync();

            await LogTransactionAsync(product.Id, null, dto.TransactionType, signedQuantity, stockBefore, stockAfter, dto.Notes, currentUsername);
        }

        Log.Information("Manual stock {Type} of {Quantity} ({Direction}) recorded by '{User}' for ProductId {ProductId}, VariantId {VariantId}.",
            dto.TransactionType, dto.Quantity, dto.IsIncrease ? "increase" : "decrease", currentUsername, dto.ProductId, dto.ProductVariantId);

        await _auditLogService.LogAsync(AuditModules.Inventory, dto.TransactionType.ToString(),
            $"{(dto.IsIncrease ? "Increased" : "Decreased")} stock by {dto.Quantity} for ProductId {dto.ProductId}" +
            (dto.ProductVariantId.HasValue ? $" (VariantId {dto.ProductVariantId})" : "") +
            (string.IsNullOrWhiteSpace(dto.Notes) ? "." : $" — {dto.Notes}"));

        return OperationResult.Ok();
    }

    private async Task LogTransactionAsync(
        int productId, int? variantId, InventoryTransactionType type,
        decimal signedQuantity, decimal stockBefore, decimal stockAfter, string? notes, string username)
    {
        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            ProductVariantId = variantId,
            TransactionType = type,
            Quantity = signedQuantity,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            ReferenceId = null,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = username
        };

        await _transactionRepository.AddAsync(transaction);
        await _transactionRepository.SaveChangesAsync();
    }

    private static InventoryItemDto MapProductToDto(Product product)
    {
        return new InventoryItemDto
        {
            ProductId = product.Id,
            ProductVariantId = null,
            DisplayName = product.Name,
            CategoryName = product.Category?.Name ?? string.Empty,
            Barcode = product.Barcode,
            CurrentStock = product.CurrentStock,
            MinimumStock = product.MinimumStock
        };
    }

    private static InventoryItemDto MapVariantToDto(Product product, ProductVariant variant)
    {
        return new InventoryItemDto
        {
            ProductId = product.Id,
            ProductVariantId = variant.Id,
            DisplayName = $"{product.Name} — {variant.VariantName}",
            CategoryName = product.Category?.Name ?? string.Empty,
            Barcode = variant.Barcode,
            CurrentStock = variant.CurrentStock,
            MinimumStock = variant.MinimumStock
        };
    }

    private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction t)
    {
        return new InventoryTransactionDto
        {
            TransactionType = t.TransactionType,
            Quantity = t.Quantity,
            StockBefore = t.StockBefore,
            StockAfter = t.StockAfter,
            Notes = t.Notes,
            TransactionDate = t.TransactionDate,
            CreatedBy = t.CreatedBy
        };
    }
}