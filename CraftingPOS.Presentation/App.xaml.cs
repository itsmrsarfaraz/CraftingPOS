using System.IO;
using System.Windows;
using CraftingPOS.Application;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Infrastructure;
using CraftingPOS.Infrastructure.Logging;
using CraftingPOS.Persistence;
using CraftingPOS.Persistence.Seed;
using CraftingPOS.Presentation.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WpfApplication = System.Windows.Application;

namespace CraftingPOS.Presentation;

public partial class App : WpfApplication
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CraftingPOS");
        var logDirectory = Path.Combine(dataDirectory, "Logs");

        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(logDirectory);

        LoggingSetup.ConfigureSerilog(logDirectory);
        Log.Information("CraftingPOS starting up...");

        var dbPath = Path.Combine(dataDirectory, "CraftingPOS.db");

        AppHost = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration(config =>
            {
                var inMemorySettings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}"
                };
                config.AddInMemoryCollection(inMemorySettings);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPersistence(context.Configuration);
                services.AddApplicationServices();
                services.AddInfrastructureServices();
                services.AddPresentationServices();
            })
            .Build();

        await AppHost.StartAsync();

        using (var scope = AppHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await DataSeeder.SeedAsync(db, passwordHasher.Hash);
        }

        Log.Information("CraftingPOS startup complete. Showing login screen.");

        var loginView = AppHost.Services.GetRequiredService<LoginView>();
        loginView.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("CraftingPOS shutting down...");
        Log.CloseAndFlush();
        await AppHost.StopAsync();
        base.OnExit(e);
    }
}