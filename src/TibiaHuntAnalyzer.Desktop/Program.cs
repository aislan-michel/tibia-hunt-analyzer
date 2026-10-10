using Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TibiaHuntAnalyzer.Desktop.ViewModels;
using TibiaHuntAnalyzer.Desktop.Views;
using TibiaHuntAnalyzer.Infrastructure;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Application.Imports;
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
        using var services = CreateServiceProvider();
        using (var dbContext = services.GetRequiredService<IDbContextFactory<HuntAnalyzerDbContext>>()
                   .CreateDbContext())
        {
            dbContext.Database.Migrate();
        }

        BuildAvaloniaApp(services).StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => BuildAvaloniaApp(CreateServiceProvider());

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure();
        services.AddSingleton<JsonHuntSessionParser>();
        services.AddSingleton<TextHuntSessionParser>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }
}
