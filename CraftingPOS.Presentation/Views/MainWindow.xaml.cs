using System.Windows;
using CraftingPOS.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += (_, _) => ShowDashboard();
    }

    private void DashboardButton_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void SalesButton_Click(object sender, RoutedEventArgs e) => ShowSales();
    private void CategoriesButton_Click(object sender, RoutedEventArgs e) => ShowCategories();
    private void BrandsButton_Click(object sender, RoutedEventArgs e) => ShowBrands();
    private void ProductsButton_Click(object sender, RoutedEventArgs e) => ShowProducts();
    private void InventoryButton_Click(object sender, RoutedEventArgs e) => ShowInventory();
    private void SuppliersButton_Click(object sender, RoutedEventArgs e) => ShowSuppliers();
    private void PurchasesButton_Click(object sender, RoutedEventArgs e) => ShowPurchases();
    private void CustomersButton_Click(object sender, RoutedEventArgs e) => ShowCustomers();
    private void ReportsButton_Click(object sender, RoutedEventArgs e) => ShowReports();

    private void ShowDashboard() => MainContent.Content = App.AppHost.Services.GetRequiredService<DashboardView>();
    private void ShowSales() => MainContent.Content = App.AppHost.Services.GetRequiredService<PosView>();
    private void ShowCategories() => MainContent.Content = App.AppHost.Services.GetRequiredService<CategoriesView>();
    private void ShowBrands() => MainContent.Content = App.AppHost.Services.GetRequiredService<BrandsView>();
    private void ShowProducts() => MainContent.Content = App.AppHost.Services.GetRequiredService<ProductsView>();
    private void ShowInventory() => MainContent.Content = App.AppHost.Services.GetRequiredService<InventoryView>();
    private void ShowSuppliers() => MainContent.Content = App.AppHost.Services.GetRequiredService<SuppliersView>();
    private void ShowPurchases() => MainContent.Content = App.AppHost.Services.GetRequiredService<PurchasesView>();
    private void ShowCustomers() => MainContent.Content = App.AppHost.Services.GetRequiredService<CustomersView>();
    private void ShowReports() => MainContent.Content = App.AppHost.Services.GetRequiredService<ReportsView>();
}