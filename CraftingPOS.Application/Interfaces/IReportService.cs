using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IReportService
{
    /// <summary>FR-REP-001: Daily/ranged Sales Report.</summary>
    Task<SalesReportDto> GetSalesReportAsync(DateTime fromDate, DateTime toDate);

    /// <summary>FR-REP-002: Daily/ranged Profit Report.</summary>
    Task<ProfitReportDto> GetProfitReportAsync(DateTime fromDate, DateTime toDate);

    /// <summary>FR-REP-004/005: Current Stock and Low Stock Report (reuses Sprint 9's IInventoryService).</summary>
    Task<StockReportDto> GetStockReportAsync(bool lowStockOnly);

    /// <summary>FR-REP-007/008: Best Selling / Slow Moving Products.</summary>
    Task<ProductPerformanceReportDto> GetProductPerformanceReportAsync(DateTime fromDate, DateTime toDate, int topCount = 10);

    /// <summary>FR-REP-009: Outstanding Customer Balances.</summary>
    Task<CustomerBalanceReportDto> GetCustomerBalancesReportAsync();
}