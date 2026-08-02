using System.IO;
using System.Windows;
using System.Windows.Threading;
using CraftingPOS.Application;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Infrastructure;
using CraftingPOS.Infrastructure.Logging;
using CraftingPOS.Licensing;
using CraftingPOS.Persistence;
using CraftingPOS.Persistence.Seed;
using CraftingPOS.Presentation.ViewModels;
using CraftingPOS.Presentation.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CraftingPOS.Presentation;

public partial class App : System.Windows.Application
{
    public static IHost AppHost { get; private set; } = null!;
    public static LicenseManager LicenseManagerInstance { get; private set; } = null!;

    private string _dataDirectory = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CraftingPOS");
        var logDirectory = Path.Combine(_dataDirectory, "Logs");

        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(logDirectory);

        LoggingSetup.ConfigureSerilog(logDirectory);
        Log.Information("CraftingPOS starting up...");

        LicenseManagerInstance = new LicenseManager(_dataDirectory);
        var licenseResult = LicenseManagerInstance.Validate();

        if (!licenseResult.IsValid)
        {
            // FR-LIC-004: refuse execution until a valid license is activated.
            Log.Warning("License invalid at startup: {Reason}", licenseResult.ErrorMessage);
            ShowActivationWindow();
            return;
        }

        _ = ContinueStartupAsync();

        base.OnStartup(e);
    }

    private void ShowActivationWindow()
    {
        var viewModel = new ActivationViewModel(LicenseManagerInstance);
        var window = new ActivationWindow(viewModel);

        viewModel.ActivationSucceeded += async () =>
        {
            window.Close();
            await ContinueStartupAsync();
        };

        window.Show();
    }

    private async Task ContinueStartupAsync()
    {
        var dbPath = Path.Combine(_dataDirectory, "CraftingPOS.db");

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
                services.AddSingleton(LicenseManagerInstance);
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
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI exception.");
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nSee the log file for details.",
            "CraftingPOS - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
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

        if (AppHost != null)
        {
            await AppHost.StopAsync();
        }

        base.OnExit(e);
    }
}