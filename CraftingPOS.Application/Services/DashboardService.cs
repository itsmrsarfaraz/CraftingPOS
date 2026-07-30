using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

/// <summary>
/// Aggregates dashboard statistics.
///
/// IMPORTANT (temporary, Sprint 2 only):
/// Products, Customers, Suppliers, and Sales repositories do not exist yet
/// (they are introduced in Sprints 4, 5, 7, and 10 respectively).
/// Until then this service returns zeroed/empty values for those sections
/// so the Dashboard UI is fully functional today.
///
/// Each future sprint MUST replace its corresponding TODO block with a
/// real repository call. No other part of the Dashboard needs to change.
/// </summary>
public class DashboardService : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var summary = new DashboardSummaryDto();

        try
        {
            // TODO (Sprint 10 - Sales): replace with real SUM(GrandTotal) for today
            summary.TodaysSales = 0m;

            // TODO (Sprint 10 - Sales): replace with real profit calculation for today
            summary.TodaysProfit = 0m;

            // TODO (Sprint 4 - Products): replace with real Products.Count()
            summary.TotalProducts = 0;

            // TODO (Sprint 7 - Customers): replace with real Customers.Count()
            summary.TotalCustomers = 0;

            // TODO (Sprint 5 - Suppliers): replace with real Suppliers.Count()
            summary.TotalSuppliers = 0;

            // TODO (Sprint 9 - Inventory): replace with real low stock query
            summary.LowStockItems = new List<LowStockItemDto>();

            // TODO (Sprint 10 - Sales): replace with real recent sales query
            summary.RecentSales = new List<RecentSaleDto>();

            summary.GeneratedAt = DateTime.Now;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dashboard summary.");
            throw;
        }

        return summary;
    }
}