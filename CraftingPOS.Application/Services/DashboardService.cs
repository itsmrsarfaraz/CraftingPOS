using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;
    private readonly ICustomerService _customerService;
    private readonly IInventoryService _inventoryService;
    private readonly ISaleService _saleService;

    public DashboardService(
        IProductService productService,
        ISupplierService supplierService,
        ICustomerService customerService,
        IInventoryService inventoryService,
        ISaleService saleService)
    {
        _productService = productService;
        _supplierService = supplierService;
        _customerService = customerService;
        _inventoryService = inventoryService;
        _saleService = saleService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var summary = new DashboardSummaryDto();

        try
        {
            // Sprint 10: all remaining TODOs resolved.
            summary.TodaysSales = await _saleService.GetTodaysSalesTotalAsync();
            summary.TodaysProfit = await _saleService.GetTodaysProfitTotalAsync();
            summary.TotalProducts = await _productService.CountAsync();
            summary.TotalCustomers = await _customerService.CountAsync();
            summary.TotalSuppliers = await _supplierService.CountAsync();
            summary.LowStockItems = await _inventoryService.GetLowStockForDashboardAsync();
            summary.RecentSales = await _saleService.GetRecentSalesAsync();

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