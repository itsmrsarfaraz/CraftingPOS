using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductDiscountRepository _productDiscountRepository;
    private readonly IDiscountSettingsRepository _discountSettingsRepository;
    private readonly IInventoryTransactionRepository _inventoryTransactionRepository;
    private readonly ICustomerLedgerService _customerLedgerService;
    private readonly IAuditLogService _auditLogService;
    private readonly CurrentUserContext _currentUserContext;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IProductDiscountRepository productDiscountRepository,
        IDiscountSettingsRepository discountSettingsRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        ICustomerLedgerService customerLedgerService,
        IAuditLogService auditLogService,
        CurrentUserContext currentUserContext)
    {
        _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _variantRepository = variantRepository ?? throw new ArgumentNullException(nameof(variantRepository));
        _productDiscountRepository = productDiscountRepository ?? throw new ArgumentNullException(nameof(productDiscountRepository));
        _discountSettingsRepository = discountSettingsRepository ?? throw new ArgumentNullException(nameof(discountSettingsRepository));
        _inventoryTransactionRepository = inventoryTransactionRepository ?? throw new ArgumentNullException(nameof(inventoryTransactionRepository));
        _customerLedgerService = customerLedgerService ?? throw new ArgumentNullException(nameof(customerLedgerService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    private bool IsOwnerOrAdmin =>
        _currentUserContext.Session?.RoleName is RoleNames.Owner or RoleNames.SystemAdmin;

    public async Task<CartItemLookupDto?> FindByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var cleanBarcode = barcode.Trim();

        var product = await _productRepository.GetByBarcodeAsync(cleanBarcode);
        if (product != null)
        {
            var discount = await _productDiscountRepository.GetByProductIdAsync(product.Id);
            return BuildLookup(product.Id, null, product.Name, product.SellingPrice, product.CostPrice, product.CurrentStock, discount);
        }

        var variant = await _variantRepository.GetByBarcodeAsync(cleanBarcode);
        if (variant != null)
        {
            var discount = await _productDiscountRepository.GetByProductIdAsync(variant.ProductId);
            var productName = variant.Product?.Name ?? "Item";
            return BuildLookup(variant.ProductId, variant.Id, $"{productName} — {variant.VariantName}",
                variant.SellingPrice, variant.CostPrice, variant.CurrentStock, discount);
        }

        return null;
    }

    private static CartItemLookupDto BuildLookup(
        int productId, int? variantId, string displayName, decimal price, decimal cost, decimal stock, ProductDiscount? discount)
    {
        var effectivePrice = price;
        if (discount != null)
        {
            effectivePrice = discount.DiscountType == DiscountType.Percentage
                ? Math.Max(0, price - (price * discount.DiscountValue / 100m))
                : Math.Max(0, price - discount.DiscountValue);
        }

        return new CartItemLookupDto
        {
            ProductId = productId,
            ProductVariantId = variantId,
            DisplayName = displayName,
            OriginalUnitPrice = price,
            EffectiveUnitPrice = effectivePrice,
            UnitCost = cost,
            AvailableStock = stock
        };
    }

    public async Task<OperationResult<CompletedSaleResultDto>> CompleteSaleAsync(CompleteSaleDto dto)
    {
        if (dto == null || dto.Items == null || dto.Items.Count == 0)
            return OperationResult<CompletedSaleResultDto>.Fail("Cannot complete a sale with an empty cart.");

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
                return OperationResult<CompletedSaleResultDto>.Fail("Each item quantity must be greater than zero.");
        }

        if (dto.PaymentMethod == PaymentMethod.Credit && !dto.CustomerId.HasValue)
            return OperationResult<CompletedSaleResultDto>.Fail("Select a customer before completing a credit sale.");

        var subTotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        var itemsBelowCost = dto.Items.Where(i => i.UnitPrice < i.UnitCost).ToList();
        if (itemsBelowCost.Count > 0)
        {
            if (!IsOwnerOrAdmin)
            {
                return OperationResult<CompletedSaleResultDto>.Fail(
                    "A cashier cannot sell a product below its purchase price. Ask an Owner to complete this sale.");
            }

            if (!dto.BelowCostConfirmed)
            {
                return OperationResult<CompletedSaleResultDto>.Fail(
                    "OWNER_CONFIRM_BELOW_COST: One or more items are priced below their purchase cost. Confirm to proceed.");
            }
        }

        var cartDiscountAmount = dto.CartDiscountType == DiscountType.Percentage
            ? subTotal * dto.CartDiscountValue / 100m
            : dto.CartDiscountValue;

        if (!IsOwnerOrAdmin && !dto.DiscountOverrideAuthorized)
        {
            var settings = await _discountSettingsRepository.GetOrCreateAsync();
            var percentOfSubtotal = subTotal > 0 ? (cartDiscountAmount / subTotal) * 100m : 0m;

            if (percentOfSubtotal > settings.MaxCashierDiscountPercent || cartDiscountAmount > settings.MaxCashierDiscountFlat)
            {
                return OperationResult<CompletedSaleResultDto>.Fail(
                    $"OWNER_AUTH_REQUIRED: This discount exceeds the cashier limit " +
                    $"({settings.MaxCashierDiscountPercent}% / Rs. {settings.MaxCashierDiscountFlat:N0}). Owner authorization required.");
            }
        }

        var grandTotal = Math.Max(0, subTotal - cartDiscountAmount);
        decimal? changeDue = null;

        if (dto.PaymentMethod == PaymentMethod.Cash)
        {
            if (dto.AmountReceived < grandTotal)
                return OperationResult<CompletedSaleResultDto>.Fail(
                    $"Amount received (Rs. {dto.AmountReceived:N0}) is less than the total due (Rs. {grandTotal:N0}).");
            changeDue = dto.AmountReceived - grandTotal;
        }

        foreach (var item in dto.Items)
        {
            decimal available;
            if (item.ProductVariantId.HasValue)
            {
                var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId.Value);
                if (variant == null) return OperationResult<CompletedSaleResultDto>.Fail("A cart item no longer exists. Refresh and try again.");
                available = variant.CurrentStock;
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null) return OperationResult<CompletedSaleResultDto>.Fail("A cart item no longer exists. Refresh and try again.");
                available = product.CurrentStock;
            }

            if (item.Quantity > available)
                return OperationResult<CompletedSaleResultDto>.Fail(
                    $"Insufficient stock for one of the cart items (requested {item.Quantity}, available {available}).");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";
        var cashierId = _currentUserContext.Session?.UserId ?? 0;
        var paymentStatus = dto.PaymentMethod == PaymentMethod.Credit ? SalePaymentStatus.Credit : SalePaymentStatus.Paid;

        var salesCountToday = await _saleRepository.CountAsync();
        var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{(salesCountToday + 1):D5}";

        var sale = new Sale
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = dto.CustomerId,
            CashierId = cashierId,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            ProductDiscount = 0,
            CartDiscount = cartDiscountAmount,
            TaxAmount = 0,
            GrandTotal = grandTotal,
            PaymentStatus = paymentStatus,
            Notes = dto.Notes?.Trim(),
            CreatedBy = currentUsername
        };

        foreach (var item in dto.Items)
        {
            sale.Items.Add(new SaleItem
            {
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitCost = item.UnitCost,
                DiscountAmount = 0,
                LineTotal = item.Quantity * item.UnitPrice,
                CreatedBy = currentUsername
            });
        }

        sale.Payments.Add(new Payment
        {
            PaymentMethod = dto.PaymentMethod,
            Amount = dto.PaymentMethod == PaymentMethod.Credit ? 0 : grandTotal,
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            PaymentDate = DateTime.UtcNow,
            CreatedBy = currentUsername
        });

        await _saleRepository.AddAsync(sale);
        await _saleRepository.SaveChangesAsync();

        foreach (var item in sale.Items)
        {
            if (item.ProductVariantId.HasValue)
            {
                var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId.Value);
                if (variant != null)
                {
                    var stockBefore = variant.CurrentStock;
                    variant.CurrentStock -= item.Quantity;
                    await _variantRepository.UpdateAsync(variant);

                    await LogInventoryTransactionAsync(item.ProductId, variant.Id, InventoryTransactionType.Sale,
                        -item.Quantity, stockBefore, variant.CurrentStock, sale.Id, $"Sale Invoice: {sale.InvoiceNumber}", currentUsername);
                }
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    var stockBefore = product.CurrentStock;
                    product.CurrentStock -= item.Quantity;
                    await _productRepository.UpdateAsync(product);

                    await LogInventoryTransactionAsync(product.Id, null, InventoryTransactionType.Sale,
                        -item.Quantity, stockBefore, product.CurrentStock, sale.Id, $"Sale Invoice: {sale.InvoiceNumber}", currentUsername);
                }
            }
        }

        await _variantRepository.SaveChangesAsync();
        await _productRepository.SaveChangesAsync();

        if (dto.PaymentMethod == PaymentMethod.Credit && dto.CustomerId.HasValue)
        {
            await _customerLedgerService.RecordCreditSaleAsync(
                dto.CustomerId.Value, sale.Id, grandTotal, $"Credit sale (Invoice: {sale.InvoiceNumber})");
        }

        Log.Information("Sale '{Invoice}' completed by '{User}'. Total: {Total}, Method: {Method}",
            sale.InvoiceNumber, currentUsername, sale.GrandTotal, dto.PaymentMethod);

        await _auditLogService.LogAsync(AuditModules.Sales, "Complete",
            $"Sale '{sale.InvoiceNumber}' completed. Total: Rs. {sale.GrandTotal:N0}, Method: {dto.PaymentMethod}." +
            (dto.BelowCostConfirmed ? " (Owner-confirmed below-cost sale)" : "") +
            (dto.DiscountOverrideAuthorized && !IsOwnerOrAdmin ? " (Owner-authorized discount override)" : ""));

        return OperationResult<CompletedSaleResultDto>.Ok(new CompletedSaleResultDto
        {
            SaleId = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            GrandTotal = sale.GrandTotal,
            ChangeDue = changeDue
        });
    }

    public async Task<decimal> GetTodaysSalesTotalAsync()
    {
        var sales = await _saleRepository.GetTodaysSalesAsync();
        return sales.Sum(s => s.GrandTotal);
    }

    public async Task<decimal> GetTodaysProfitTotalAsync()
    {
        var sales = await _saleRepository.GetTodaysSalesAsync();
        return sales.Sum(s => s.Items.Sum(i => (i.UnitPrice - i.UnitCost) * i.Quantity));
    }

    public async Task<List<RecentSaleDto>> GetRecentSalesAsync(int count = 5)
    {
        var sales = await _saleRepository.GetRecentAsync(count);
        return sales.Select(s => new RecentSaleDto
        {
            InvoiceNumber = s.InvoiceNumber,
            SaleDate = s.SaleDate,
            GrandTotal = s.GrandTotal,
            CashierName = s.Cashier?.FullName ?? string.Empty
        }).ToList();
    }

    public async Task<List<SalesHistoryItemDto>> GetSalesHistoryForCustomerAsync(int customerId)
    {
        var sales = await _saleRepository.GetByCustomerIdAsync(customerId);
        return sales.OrderByDescending(s => s.SaleDate).Select(s => new SalesHistoryItemDto
        {
            InvoiceNumber = s.InvoiceNumber,
            SaleDate = s.SaleDate,
            GrandTotal = s.GrandTotal,
            PaymentStatus = s.PaymentStatus.ToString()
        }).ToList();
    }

    public async Task<ReceiptDto?> GetReceiptAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId);
        if (sale == null) return null;

        var payment = sale.Payments.FirstOrDefault();

        return new ReceiptDto
        {
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate = sale.SaleDate,
            CashierName = sale.Cashier?.FullName ?? string.Empty,
            CustomerName = sale.Customer?.Name,
            Items = sale.Items.Select(i => new ReceiptLineDto
            {
                ProductName = i.ProductVariant != null
                    ? $"{i.Product?.Name} — {i.ProductVariant.VariantName}"
                    : i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList(),
            SubTotal = sale.SubTotal,
            Discount = sale.CartDiscount + sale.ProductDiscount,
            GrandTotal = sale.GrandTotal,
            PaymentMethod = payment?.PaymentMethod.ToString() ?? sale.PaymentStatus.ToString(),
            AmountReceived = payment?.Amount,
            ChangeDue = payment != null && payment.Amount > sale.GrandTotal ? payment.Amount - sale.GrandTotal : null
        };
    }

    private async Task LogInventoryTransactionAsync(
        int productId, int? variantId, InventoryTransactionType type,
        decimal signedQuantity, decimal stockBefore, decimal stockAfter, int referenceId, string notes, string username)
    {
        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            ProductVariantId = variantId,
            TransactionType = type,
            Quantity = signedQuantity,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTime.UtcNow,
            CreatedBy = username
        };

        await _inventoryTransactionRepository.AddAsync(transaction);
    }
}