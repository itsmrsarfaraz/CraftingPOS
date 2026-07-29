using System.IO;
using System.Windows;
using CraftingPOS.Infrastructure.Logging;
using CraftingPOS.Persistence;
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
        // Data + log directory: C:\ProgramData\CraftingPOS\
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
                // Future sprints will add:
                // services.AddApplicationServices();
                // services.AddInfrastructureServices();
                // services.AddViewModelsAndViews();
            })
            .Build();

        await AppHost.StartAsync();

        // Ensure database + tables exist on first run
        using (var scope = AppHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        Log.Information("CraftingPOS startup complete.");

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