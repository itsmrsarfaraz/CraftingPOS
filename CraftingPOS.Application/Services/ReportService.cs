using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Interfaces;

namespace CraftingPOS.Application.Services;

public class ReportService : IReportService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerLedgerService _customerLedgerService;

    public ReportService(
        ISaleRepository saleRepository,
        IInventoryService inventoryService,
        ICustomerRepository customerRepository,
        ICustomerLedgerService customerLedgerService)
    {
        _saleRepository = saleRepository;
        _inventoryService = inventoryService;
        _customerRepository = customerRepository;
        _customerLedgerService = customerLedgerService;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
    {
        var allSales = await _saleRepository.GetAllAsync();
        var fromUtc = fromDate.Date;
        var toUtc = toDate.Date.AddDays(1);

        var filtered = allSales.Where(s => s.SaleDate >= fromUtc && s.SaleDate < toUtc);

        return new SalesReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = filtered.OrderBy(s => s.SaleDate).Select(s => new SalesReportRowDto
            {
                InvoiceNumber = s.InvoiceNumber,
                SaleDate = s.SaleDate,
                CashierName = s.Cashier?.FullName ?? string.Empty,
                CustomerName = s.Customer?.Name,
                SubTotal = s.SubTotal,
                Discount = s.CartDiscount + s.ProductDiscount,
                GrandTotal = s.GrandTotal,
                PaymentStatus = s.PaymentStatus.ToString()
            }).ToList()
        };
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(DateTime fromDate, DateTime toDate)
    {
        var allSales = await _saleRepository.GetAllAsync();
        var fromUtc = fromDate.Date;
        var toUtc = toDate.Date.AddDays(1);

        var filtered = allSales.Where(s => s.SaleDate >= fromUtc && s.SaleDate < toUtc);

        return new ProfitReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Rows = filtered.OrderBy(s => s.SaleDate).Select(s => new ProfitReportRowDto
            {
                SaleDate = s.SaleDate,
                InvoiceNumber = s.InvoiceNumber,
                Revenue = s.GrandTotal,
                Cost = s.Items.Sum(i => i.UnitCost * i.Quantity)
            }).ToList()
        };
    }

    public async Task<StockReportDto> GetStockReportAsync(bool lowStockOnly)
    {
        var items = lowStockOnly
            ? await _inventoryService.GetLowStockAsync()
            : await _inventoryService.GetAllAsync();

        return new StockReportDto
        {
            Rows = items.Select(i => new StockReportRowDto
            {
                ProductName = i.DisplayName,
                CategoryName = i.CategoryName,
                CurrentStock = i.CurrentStock,
                MinimumStock = i.MinimumStock,
                Status = i.Status.ToString()
            }).ToList()
        };
    }

    public async Task<ProductPerformanceReportDto> GetProductPerformanceReportAsync(DateTime fromDate, DateTime toDate, int topCount = 10)
    {
        var allSales = await _saleRepository.GetAllAsync();
        var fromUtc = fromDate.Date;
        var toUtc = toDate.Date.AddDays(1);

        var lineItems = allSales
            .Where(s => s.SaleDate >= fromUtc && s.SaleDate < toUtc)
            .SelectMany(s => s.Items)
            .ToList();

        var grouped = lineItems
            .GroupBy(i => i.ProductVariant != null
                ? $"{i.Product?.Name} — {i.ProductVariant.VariantName}"
                : i.Product?.Name ?? "Unknown")
            .Select(g => new ProductPerformanceRowDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .ToList();

        return new ProductPerformanceReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            BestSelling = grouped.OrderByDescending(g => g.QuantitySold).Take(topCount).ToList(),
            SlowMoving = grouped.OrderBy(g => g.QuantitySold).Take(topCount).ToList()
        };
    }

    public async Task<CustomerBalanceReportDto> GetCustomerBalancesReportAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        var rows = new List<CustomerBalanceRowDto>();

        foreach (var c in customers)
        {
            var balance = await _customerLedgerService.GetOutstandingBalanceAsync(c.Id);
            if (balance != 0)
            {
                rows.Add(new CustomerBalanceRowDto
                {
                    CustomerName = c.Name,
                    Phone = c.Phone,
                    OutstandingBalance = balance
                });
            }
        }

        return new CustomerBalanceReportDto
        {
            Rows = rows.OrderByDescending(r => r.OutstandingBalance).ToList()
        };
    }
}