using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TibiaHuntAnalyzer.Desktop.ViewModels;
using TibiaHuntAnalyzer.Desktop.Views;
using TibiaHuntAnalyzer.Infrastructure;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Backups;
using System.IO;
using System;

namespace TibiaHuntAnalyzer.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var configurationPath = ApplicationDataPaths.GetConfigurationFilePath();
        var isExplicitlyConfigured = File.Exists(configurationPath);
        string databasePath;
        DatabaseStartupStatus startupStatus;
        try
        {
            databasePath = ApplicationDataPaths.GetDatabasePath();
            startupStatus = new DatabaseStartupStatus();
        }
        catch (Exception exception)
        {
            databasePath = ApplicationDataPaths.GetDefaultDatabasePath();
            startupStatus = new DatabaseStartupStatus(
                $"Could not read the configured database path: {exception.Message}");
        }

        using var services = CreateServiceProvider(databasePath, startupStatus);
        if (!startupStatus.HasError)
        {
            try
            {
                DatabasePathCompatibility
                    .InitializeConfiguredDatabaseAsync(databasePath, isExplicitlyConfigured)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                startupStatus.ReportError(
                    $"Could not open the configured database at '{databasePath}': {exception.Message}");
            }
        }

        BuildAvaloniaApp(services).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var configurationPath = ApplicationDataPaths.GetConfigurationFilePath();
        var databasePath = ApplicationDataPaths.GetDatabasePath();
        return BuildAvaloniaApp(CreateServiceProvider(
            databasePath,
            new DatabaseStartupStatus()));
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static ServiceProvider CreateServiceProvider(
        string databasePath,
        DatabaseStartupStatus startupStatus)
    {
        var services = new ServiceCollection();
        services.AddSingleton(startupStatus);
        services.AddInfrastructure(databasePath);
        services.AddSingleton<JsonHuntSessionParser>();
        services.AddSingleton<TextHuntSessionParser>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }
}
