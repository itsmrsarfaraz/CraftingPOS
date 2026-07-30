using CraftingPOS.Application.Common;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<CurrentUserContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<ISupplierService, SupplierService>();

        return services;
    }
}