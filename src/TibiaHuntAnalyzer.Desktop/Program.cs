using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using TibiaHuntAnalyzer.Desktop.ViewModels;
using TibiaHuntAnalyzer.Desktop.Views;
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
        => new ServiceCollection()
            .AddSingleton<MainViewModel>()
            .BuildServiceProvider();
}
