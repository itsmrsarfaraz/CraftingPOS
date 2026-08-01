namespace CraftingPOS.Application.DTOs;

public class SalesReportRowDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public class SalesReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<SalesReportRowDto> Rows { get; set; } = new();
    public decimal TotalSales => Rows.Sum(r => r.GrandTotal);
    public decimal TotalDiscount => Rows.Sum(r => r.Discount);
    public int TotalTransactions => Rows.Count;
}

public class ProfitReportRowDto
{
    public DateTime SaleDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit => Revenue - Cost;
}

public class ProfitReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ProfitReportRowDto> Rows { get; set; } = new();
    public decimal TotalRevenue => Rows.Sum(r => r.Revenue);
    public decimal TotalCost => Rows.Sum(r => r.Cost);
    public decimal TotalProfit => Rows.Sum(r => r.Profit);
}

public class StockReportRowDto
{
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StockReportDto
{
    public List<StockReportRowDto> Rows { get; set; } = new();
}

public class ProductPerformanceRowDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class ProductPerformanceReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ProductPerformanceRowDto> BestSelling { get; set; } = new();
    public List<ProductPerformanceRowDto> SlowMoving { get; set; } = new();
}

public class CustomerBalanceRowDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class CustomerBalanceReportDto
{
    public List<CustomerBalanceRowDto> Rows { get; set; } = new();
    public decimal TotalOutstanding => Rows.Sum(r => r.OutstandingBalance);
}

public enum ReportKind
{
    Sales,
    Profit,
    Stock,
    ProductPerformance,
    CustomerBalances
}

public enum ExportFormat
{
    Pdf,
    Excel
}