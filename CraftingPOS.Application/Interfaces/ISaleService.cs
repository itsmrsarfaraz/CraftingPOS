using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ISaleService
{
    Task<CartItemLookupDto?> FindByBarcodeAsync(string barcode);
    Task<OperationResult<CompletedSaleResultDto>> CompleteSaleAsync(CompleteSaleDto dto);
    Task<decimal> GetTodaysSalesTotalAsync();
    Task<decimal> GetTodaysProfitTotalAsync();
    Task<List<RecentSaleDto>> GetRecentSalesAsync(int count = 5);
    Task<List<SalesHistoryItemDto>> GetSalesHistoryForCustomerAsync(int customerId);

    /// <summary>FR-PRINT-004: builds receipt content for a completed sale, immediately after checkout.</summary>
    Task<ReceiptDto?> GetReceiptAsync(int saleId);
}