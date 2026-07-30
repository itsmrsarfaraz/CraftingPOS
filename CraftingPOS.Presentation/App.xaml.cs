using System.IO;
using System.Windows;
using System.Windows.Threading;
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

namespace CraftingPOS.Presentation;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Global exception handler: log crashes instead of failing silently.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception.");
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nSee the log file for details.",
            "CraftingPOS - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true; // prevent silent crash; keep app running
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log.Fatal(ex, "Unhandled non-UI exception. Application will terminate.");
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("CraftingPOS shutting down...");
        Log.CloseAndFlush();
        await AppHost.StopAsync();
        base.OnExit(e);
    }
}