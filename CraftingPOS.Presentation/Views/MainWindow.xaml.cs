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

        Loaded += (_, _) =>
        {
            var dashboardView = App.AppHost.Services.GetRequiredService<DashboardView>();
            MainContent.Content = dashboardView;
        };
    }
}