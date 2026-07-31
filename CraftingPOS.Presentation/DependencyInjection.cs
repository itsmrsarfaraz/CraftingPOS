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

        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CategoriesView>();

        services.AddTransient<BrandsViewModel>();
        services.AddTransient<BrandsView>();

        services.AddTransient<ProductsViewModel>();
        services.AddTransient<ProductsView>();

        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<SuppliersView>();

        services.AddTransient<PurchasesViewModel>();
        services.AddTransient<PurchasesView>();

        services.AddTransient<CustomersViewModel>();
        services.AddTransient<CustomersView>();

        return services;
    }
}