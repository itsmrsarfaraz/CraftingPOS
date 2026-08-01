using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ISaleService
{
    /// <summary>FR-BAR-003: resolves a scanned barcode to a product or variant for the cart.</summary>
    Task<CartItemLookupDto?> FindByBarcodeAsync(string barcode);

    /// <summary>FR-SALE-006 / BR-SALE-001/002: creates Sale + SaleItems + Payment, deducts stock, logs Khata if Credit.</summary>
    Task<OperationResult<CompletedSaleResultDto>> CompleteSaleAsync(CompleteSaleDto dto);

    /// <summary>Dashboard: Today's Sales stat card.</summary>
    Task<decimal> GetTodaysSalesTotalAsync();

    /// <summary>Dashboard: Today's Profit stat card.</summary>
    Task<decimal> GetTodaysProfitTotalAsync();

    /// <summary>Dashboard: Recent Sales panel.</summary>
    Task<List<RecentSaleDto>> GetRecentSalesAsync(int count = 5);

    /// <summary>Customer screen: real Purchase History (replaces Sprint 7's placeholder).</summary>
    Task<List<SalesHistoryItemDto>> GetSalesHistoryForCustomerAsync(int customerId);
}