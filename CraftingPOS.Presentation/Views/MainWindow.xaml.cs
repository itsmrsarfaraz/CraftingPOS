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
    private void CategoriesButton_Click(object sender, RoutedEventArgs e) => ShowCategories();
    private void ProductsButton_Click(object sender, RoutedEventArgs e) => ShowProducts();
    private void SuppliersButton_Click(object sender, RoutedEventArgs e) => ShowSuppliers();

    private void ShowDashboard()
    {
        var view = App.AppHost.Services.GetRequiredService<DashboardView>();
        MainContent.Content = view;
    }

    private void ShowCategories()
    {
        var view = App.AppHost.Services.GetRequiredService<CategoriesView>();
        MainContent.Content = view;
    }

    private void ShowProducts()
    {
        var view = App.AppHost.Services.GetRequiredService<ProductsView>();
        MainContent.Content = view;
    }

    private void ShowSuppliers()
    {
        var view = App.AppHost.Services.GetRequiredService<SuppliersView>();
        MainContent.Content = view;
    }
}