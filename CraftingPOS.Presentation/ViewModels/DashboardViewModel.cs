using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace CraftingPOS.Presentation.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private decimal todaysSales;

    [ObservableProperty]
    private decimal todaysProfit;

    [ObservableProperty]
    private int totalProducts;

    [ObservableProperty]
    private int totalCustomers;

    [ObservableProperty]
    private int totalSuppliers;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string currentDateTime = DateTime.Now.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt");

    public ObservableCollection<LowStockItemDto> LowStockItems { get; } = new();
    public ObservableCollection<RecentSaleDto> RecentSales { get; } = new();

    public bool HasLowStockItems => LowStockItems.Count > 0;
    public bool HasRecentSales => RecentSales.Count > 0;

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) =>
            CurrentDateTime = DateTime.Now.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt");
        _clockTimer.Start();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        try
        {
            var summary = await _dashboardService.GetSummaryAsync();

            TodaysSales = summary.TodaysSales;
            TodaysProfit = summary.TodaysProfit;
            TotalProducts = summary.TotalProducts;
            TotalCustomers = summary.TotalCustomers;
            TotalSuppliers = summary.TotalSuppliers;

            LowStockItems.Clear();
            foreach (var item in summary.LowStockItems)
                LowStockItems.Add(item);

            RecentSales.Clear();
            foreach (var sale in summary.RecentSales)
                RecentSales.Add(sale);

            OnPropertyChanged(nameof(HasLowStockItems));
            OnPropertyChanged(nameof(HasRecentSales));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dashboard failed to load.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}