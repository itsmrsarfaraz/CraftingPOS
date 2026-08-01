using CraftingPOS.Application.Interfaces;
using CraftingPOS.Presentation.Services;
using CraftingPOS.Presentation.ViewModels;
using CraftingPOS.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();

        services.AddTransient<PosViewModel>();
        services.AddTransient<PosView>();

        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CategoriesView>();

        services.AddTransient<BrandsViewModel>();
        services.AddTransient<BrandsView>();

        services.AddTransient<ProductsViewModel>();
        services.AddTransient<ProductsView>();

        services.AddTransient<InventoryViewModel>();
        services.AddTransient<InventoryView>();

        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SuppliersView>();

        services.AddTransient<PurchasesViewModel>();
        services.AddTransient<PurchasesView>();

        services.AddTransient<CustomersViewModel>();
        services.AddTransient<CustomersView>();

        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportsView>();

        services.AddTransient<BarcodeLabelWindow>();

        services.AddSingleton<IReceiptPrinterService, ReceiptPrinterService>();
        services.AddTransient<ReceiptPreviewViewModel>();
        services.AddTransient<ReceiptPreviewWindow>();

        return services;
    }
}