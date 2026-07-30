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

        return services;
    }
}