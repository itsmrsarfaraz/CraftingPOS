namespace CraftingPOS.Application.DTOs;

public class DashboardSummaryDto
{
    public decimal TodaysSales { get; set; }
    public decimal TodaysProfit { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }

    public List<LowStockItemDto> LowStockItems { get; set; } = new();
    public List<RecentSaleDto> RecentSales { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

public class LowStockItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
}

public class RecentSaleDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal GrandTotal { get; set; }
    public string CashierName { get; set; } = string.Empty;
}