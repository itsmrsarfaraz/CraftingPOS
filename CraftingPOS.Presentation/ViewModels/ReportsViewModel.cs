using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Microsoft.Win32;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;

    public List<ReportKind> ReportKinds { get; } = Enum.GetValues<ReportKind>().ToList();

    [ObservableProperty] private ReportKind selectedReportKind = ReportKind.Sales;
    [ObservableProperty] private DateTime fromDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime toDate = DateTime.Today;
    [ObservableProperty] private bool lowStockOnly;

    public List<string> ColumnHeaders { get; private set; } = new();
    public ObservableCollection<ObservableCollection<string>> Rows { get; } = new();

    [ObservableProperty] private string summaryLine = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public bool ShowDateRange => SelectedReportKind is ReportKind.Sales or ReportKind.Profit or ReportKind.ProductPerformance;
    public bool ShowLowStockToggle => SelectedReportKind == ReportKind.Stock;

    // Cache of the last-generated report shape, needed at export time.
    private List<string> _lastColumnHeaders = new();
    private List<List<string>> _lastRows = new();
    private string _lastTitle = string.Empty;
    private string _lastSubtitle = string.Empty;

    public ReportsViewModel(IReportService reportService, IReportExportService reportExportService)
    {
        _reportService = reportService;
        _reportExportService = reportExportService;
    }

    partial void OnSelectedReportKindChanged(ReportKind value)
    {
        OnPropertyChanged(nameof(ShowDateRange));
        OnPropertyChanged(nameof(ShowLowStockToggle));
        Rows.Clear();
        ColumnHeaders = new List<string>();
        OnPropertyChanged(nameof(ColumnHeaders));
        SummaryLine = string.Empty;
        ClearStatus();
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        ClearStatus();
        IsBusy = true;

        try
        {
            switch (SelectedReportKind)
            {
                case ReportKind.Sales:
                    await GenerateSalesReportAsync();
                    break;
                case ReportKind.Profit:
                    await GenerateProfitReportAsync();
                    break;
                case ReportKind.Stock:
                    await GenerateStockReportAsync();
                    break;
                case ReportKind.ProductPerformance:
                    await GenerateProductPerformanceReportAsync();
                    break;
                case ReportKind.CustomerBalances:
                    await GenerateCustomerBalancesReportAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate report {ReportKind}.", SelectedReportKind);
            SetStatus("Failed to generate report.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateSalesReportAsync()
    {
        var report = await _reportService.GetSalesReportAsync(FromDate, ToDate);

        _lastColumnHeaders = new List<string> { "Invoice #", "Date", "Cashier", "Customer", "Subtotal", "Discount", "Total", "Status" };
        _lastRows = report.Rows.Select(r => new List<string>
        {
            r.InvoiceNumber, r.SaleDate.ToString("dd MMM yyyy HH:mm"), r.CashierName, r.CustomerName ?? "-",
            r.SubTotal.ToString("N0"), r.Discount.ToString("N0"), r.GrandTotal.ToString("N0"), r.PaymentStatus
        }).ToList();

        _lastTitle = "Sales Report";
        _lastSubtitle = $"{FromDate:dd MMM yyyy} to {ToDate:dd MMM yyyy}";

        BindRows();
        SummaryLine = $"{report.TotalTransactions} transaction(s) — Total Sales: Rs. {report.TotalSales:N0} — Total Discount: Rs. {report.TotalDiscount:N0}";
    }

    private async Task GenerateProfitReportAsync()
    {
        var report = await _reportService.GetProfitReportAsync(FromDate, ToDate);

        _lastColumnHeaders = new List<string> { "Date", "Invoice #", "Revenue", "Cost", "Profit" };
        _lastRows = report.Rows.Select(r => new List<string>
        {
            r.SaleDate.ToString("dd MMM yyyy"), r.InvoiceNumber,
            r.Revenue.ToString("N0"), r.Cost.ToString("N0"), r.Profit.ToString("N0")
        }).ToList();

        _lastTitle = "Profit Report";
        _lastSubtitle = $"{FromDate:dd MMM yyyy} to {ToDate:dd MMM yyyy}";

        BindRows();
        SummaryLine = $"Revenue: Rs. {report.TotalRevenue:N0} — Cost: Rs. {report.TotalCost:N0} — Profit: Rs. {report.TotalProfit:N0}";
    }

    private async Task GenerateStockReportAsync()
    {
        var report = await _reportService.GetStockReportAsync(LowStockOnly);

        _lastColumnHeaders = new List<string> { "Product", "Category", "Current Stock", "Minimum Stock", "Status" };
        _lastRows = report.Rows.Select(r => new List<string>
        {
            r.ProductName, r.CategoryName, r.CurrentStock.ToString("N0"), r.MinimumStock.ToString("N0"), r.Status
        }).ToList();

        _lastTitle = LowStockOnly ? "Low Stock Report" : "Current Stock Report";
        _lastSubtitle = DateTime.Now.ToString("dd MMM yyyy");

        BindRows();
        SummaryLine = $"{report.Rows.Count} item(s) listed";
    }

    private async Task GenerateProductPerformanceReportAsync()
    {
        var report = await _reportService.GetProductPerformanceReportAsync(FromDate, ToDate);

        _lastColumnHeaders = new List<string> { "Rank", "Product", "Qty Sold", "Revenue" };
        var rows = new List<List<string>>();

        rows.Add(new List<string> { "— Best Selling —", "", "", "" });
        for (var i = 0; i < report.BestSelling.Count; i++)
        {
            var p = report.BestSelling[i];
            rows.Add(new List<string> { (i + 1).ToString(), p.ProductName, p.QuantitySold.ToString("N0"), p.Revenue.ToString("N0") });
        }

        rows.Add(new List<string> { "— Slow Moving —", "", "", "" });
        for (var i = 0; i < report.SlowMoving.Count; i++)
        {
            var p = report.SlowMoving[i];
            rows.Add(new List<string> { (i + 1).ToString(), p.ProductName, p.QuantitySold.ToString("N0"), p.Revenue.ToString("N0") });
        }

        _lastRows = rows;
        _lastTitle = "Product Performance Report";
        _lastSubtitle = $"{FromDate:dd MMM yyyy} to {ToDate:dd MMM yyyy}";

        BindRows();
        SummaryLine = $"Top {report.BestSelling.Count} best-selling and {report.SlowMoving.Count} slow-moving products shown";
    }

    private async Task GenerateCustomerBalancesReportAsync()
    {
        var report = await _reportService.GetCustomerBalancesReportAsync();

        _lastColumnHeaders = new List<string> { "Customer", "Phone", "Outstanding Balance" };
        _lastRows = report.Rows.Select(r => new List<string>
        {
            r.CustomerName, r.Phone ?? "-", r.OutstandingBalance.ToString("N0")
        }).ToList();

        _lastTitle = "Outstanding Customer Balances";
        _lastSubtitle = DateTime.Now.ToString("dd MMM yyyy");

        BindRows();
        SummaryLine = $"{report.Rows.Count} customer(s) with a balance — Total Outstanding: Rs. {report.TotalOutstanding:N0}";
    }

    private void BindRows()
    {
        ColumnHeaders = _lastColumnHeaders;
        OnPropertyChanged(nameof(ColumnHeaders));

        Rows.Clear();
        foreach (var row in _lastRows)
        {
            Rows.Add(new ObservableCollection<string>(row));
        }
    }

    [RelayCommand]
    private void ExportPdf()
    {
        if (_lastRows.Count == 0)
        {
            SetStatus("Generate a report first.", true);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"{_lastTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            _reportExportService.ExportToPdf(_lastTitle, _lastSubtitle, _lastColumnHeaders, _lastRows, dialog.FileName);
            SetStatus($"Exported to {dialog.FileName}", false);
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (_lastRows.Count == 0)
        {
            SetStatus("Generate a report first.", true);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"{_lastTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            _reportExportService.ExportToExcel(_lastTitle, _lastColumnHeaders, _lastRows, dialog.FileName);
            SetStatus($"Exported to {dialog.FileName}", false);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        HasError = isError;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        HasError = false;
    }
}