using CraftingPOS.Application.Interfaces;
using CraftingPOS.Infrastructure.Security;
using CraftingPOS.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IImageStorageService, LocalImageStorageService>();

        return services;
    }
}