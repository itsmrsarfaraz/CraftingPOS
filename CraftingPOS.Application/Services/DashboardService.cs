using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;

    public DashboardService(IProductService productService, ISupplierService supplierService)
    {
        _productService = productService;
        _supplierService = supplierService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var summary = new DashboardSummaryDto();

        try
        {
            // TODO (Sprint 10 - Sales): replace with real SUM(GrandTotal) for today
            summary.TodaysSales = 0m;

            // TODO (Sprint 10 - Sales): replace with real profit calculation for today
            summary.TodaysProfit = 0m;

            summary.TotalProducts = await _productService.CountAsync();

            // TODO (Sprint 7 - Customers): replace with real Customers.Count()
            summary.TotalCustomers = 0;

            // Sprint 5: real supplier count
            summary.TotalSuppliers = await _supplierService.CountAsync();

            // TODO (Sprint 9 - Inventory): replace with real low stock query
            summary.LowStockItems = new List<LowStockItemDto>();

            // TODO (Sprint 10 - Sales): replace with real recent sales query
            summary.RecentSales = new List<RecentSaleDto>();

            summary.GeneratedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dashboard summary.");
            throw;
        }

        return summary;
    }
}