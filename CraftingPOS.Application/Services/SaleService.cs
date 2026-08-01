using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class SaleService : Interfaces.ISaleService
{
    // Reserved default cashier discount ceiling per FR-DISC-007/008.
    // TODO (future Settings sprint): make this Owner-configurable instead of hardcoded.
    private const decimal DefaultMaxCashierDiscountPercent = 5m;

    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IInventoryTransactionRepository _inventoryTransactionRepository;
    private readonly Interfaces.ICustomerLedgerService _customerLedgerService;
    private readonly CurrentUserContext _currentUserContext;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IInventoryTransactionRepository inventoryTransactionRepository,
        Interfaces.ICustomerLedgerService customerLedgerService,
        CurrentUserContext currentUserContext)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _inventoryTransactionRepository = inventoryTransactionRepository;
        _customerLedgerService = customerLedgerService;
        _currentUserContext = currentUserContext;
    }

    public async Task<CartItemLookupDto?> FindByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var product = await _productRepository.GetByBarcodeAsync(barcode.Trim());
        if (product != null)
        {
            return new CartItemLookupDto
            {
                ProductId = product.Id,
                ProductVariantId = null,
                DisplayName = product.Name,
                UnitPrice = product.SellingPrice,
                UnitCost = product.CostPrice,
                AvailableStock = product.CurrentStock
            };
        }

        var variant = await _variantRepository.GetByBarcodeAsync(barcode.Trim());
        if (variant != null)
        {
            return new CartItemLookupDto
            {
                ProductId = variant.ProductId,
                ProductVariantId = variant.Id,
                DisplayName = $"{variant.Product?.Name} — {variant.VariantName}",
                UnitPrice = variant.SellingPrice,
                UnitCost = variant.CostPrice,
                AvailableStock = variant.CurrentStock
            };
        }

        return null;
    }

    public async Task<OperationResult<CompletedSaleResultDto>> CompleteSaleAsync(CompleteSaleDto dto)
    {
        // BR-SALE-001: Sale cannot be completed with empty cart.
        if (dto.Items == null || dto.Items.Count == 0)
        {
            return OperationResult<CompletedSaleResultDto>.Fail("Cannot complete a sale with an empty cart.");
        }

        foreach (var item in dto.Items)
        {
            if (item.Quantity <= 0)
                return OperationResult<CompletedSaleResultDto>.Fail("Each item quantity must be greater than zero.");
        }

        // Credit sales require a customer to post the debt against.
        if (dto.PaymentMethod == PaymentMethod.Credit && !dto.CustomerId.HasValue)
        {
            return OperationResult<CompletedSaleResultDto>.Fail("Select a customer before completing a credit sale.");
        }

        var subTotal = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

        // FR-DISC-006/007/008: enforce cashier discount ceiling unless the session is an Owner.
        var discountPercentOfSubtotal = subTotal > 0 ? (dto.CartDiscountAmount / subTotal) * 100m : 0m;
        if (discountPercentOfSubtotal > DefaultMaxCashierDiscountPercent && !dto.AllowDiscountOverride)
        {
            return OperationResult<CompletedSaleResultDto>.Fail(
                $"This discount ({discountPercentOfSubtotal:N1}%) exceeds the {DefaultMaxCashierDiscountPercent}% cashier limit. Owner authorization required.");
        }

        var grandTotal = subTotal - dto.CartDiscountAmount;
        if (grandTotal < 0) grandTotal = 0;

        decimal? changeDue = null;

        if (dto.PaymentMethod == PaymentMethod.Cash)
        {
            if (dto.AmountReceived < grandTotal)
            {
                return OperationResult<CompletedSaleResultDto>.Fail(
                    $"Amount received (Rs. {dto.AmountReceived:N0}) is less than the total due (Rs. {grandTotal:N0}).");
            }

            changeDue = dto.AmountReceived - grandTotal;
        }

        // BR-SALE-002: Sale cannot exceed available stock — verify against live DB values.
        foreach (var item in dto.Items)
        {
            decimal available;

            if (item.ProductVariantId.HasValue)
            {
                var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId.Value);
                if (variant == null)
                    return OperationResult<CompletedSaleResultDto>.Fail("A cart item no longer exists. Please refresh and try again.");
                available = variant.CurrentStock;
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    return OperationResult<CompletedSaleResultDto>.Fail("A cart item no longer exists. Please refresh and try again.");
                available = product.CurrentStock;
            }

            if (item.Quantity > available)
            {
                return OperationResult<CompletedSaleResultDto>.Fail(
                    $"Insufficient stock for one of the cart items (requested {item.Quantity}, available {available}). Adjust the quantity.");
            }
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";
        var cashierId = _currentUserContext.Session?.UserId ?? 0;

        var paymentStatus = dto.PaymentMethod == PaymentMethod.Credit
            ? SalePaymentStatus.Credit
            : SalePaymentStatus.Paid;

        var salesCountToday = await _saleRepository.CountAsync();
        var invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{(salesCountToday + 1):D5}";

        var sale = new Sale
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = dto.CustomerId,
            CashierId = cashierId,
            SaleDate = DateTime.UtcNow,
            SubTotal = subTotal,
            ProductDiscount = 0, // reserved for future per-product discounts
            CartDiscount = dto.CartDiscountAmount,
            TaxAmount = 0, // reserved — no tax configuration yet
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
                DiscountAmount = 0, // reserved
                LineTotal = item.Quantity * item.UnitPrice,
                CreatedBy = currentUsername
            });
        }

        // FR-PAY-001/002/003: single payment record per sale in V1.
        sale.Payments.Add(new Payment
        {
            PaymentMethod = dto.PaymentMethod,
            Amount = dto.PaymentMethod == PaymentMethod.Credit ? 0 : grandTotal,
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            PaymentDate = DateTime.UtcNow,
            CreatedBy = currentUsername
        });

        await _saleRepository.AddAsync(sale);
        await _saleRepository.SaveChangesAsync(); // assigns Sale.Id and SaleItem.Ids

        // FR-SALE-006: deduct stock, and BR-INV-002: log every change as an InventoryTransaction.
        foreach (var item in sale.Items)
        {
            if (item.ProductVariantId.HasValue)
            {
                var variant = await _variantRepository.GetByIdAsync(item.ProductVariantId.Value)
                    ?? throw new InvalidOperationException($"Variant {item.ProductVariantId} vanished mid-transaction.");

                var stockBefore = variant.CurrentStock;
                variant.CurrentStock -= item.Quantity;
                await _variantRepository.UpdateAsync(variant);

                await LogInventoryTransactionAsync(
                    item.ProductId, variant.Id, InventoryTransactionType.Sale,
                    -item.Quantity, stockBefore, variant.CurrentStock, sale.Id,
                    $"Sale Invoice: {sale.InvoiceNumber}", currentUsername);
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} vanished mid-transaction.");

                var stockBefore = product.CurrentStock;
                product.CurrentStock -= item.Quantity;
                await _productRepository.UpdateAsync(product);

                await LogInventoryTransactionAsync(
                    product.Id, null, InventoryTransactionType.Sale,
                    -item.Quantity, stockBefore, product.CurrentStock, sale.Id,
                    $"Sale Invoice: {sale.InvoiceNumber}", currentUsername);
            }
        }

        await _variantRepository.SaveChangesAsync();
        await _productRepository.SaveChangesAsync();

        // FR-PAY-004: credit sales update the customer ledger (Sprint 8's Khata).
        if (dto.PaymentMethod == PaymentMethod.Credit && dto.CustomerId.HasValue)
        {
            await _customerLedgerService.RecordCreditSaleAsync(
                dto.CustomerId.Value, sale.Id, grandTotal, $"Credit sale (Invoice: {sale.InvoiceNumber})");
        }

        Log.Information("Sale '{Invoice}' completed by '{User}'. {ItemCount} item(s), Total: {Total}, Method: {Method}",
            sale.InvoiceNumber, currentUsername, sale.Items.Count, sale.GrandTotal, dto.PaymentMethod);

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

        return sales
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SalesHistoryItemDto
            {
                InvoiceNumber = s.InvoiceNumber,
                SaleDate = s.SaleDate,
                GrandTotal = s.GrandTotal,
                PaymentStatus = s.PaymentStatus.ToString()
            })
            .ToList();
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
            ChangeDue = payment != null && payment.Amount > sale.GrandTotal
                ? payment.Amount - sale.GrandTotal
                : null
        };
    }

    private async Task LogInventoryTransactionAsync(
        int productId, int? variantId, InventoryTransactionType type,
        decimal signedQuantity, decimal stockBefore, decimal stockAfter,
        int referenceId, string notes, string username)
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