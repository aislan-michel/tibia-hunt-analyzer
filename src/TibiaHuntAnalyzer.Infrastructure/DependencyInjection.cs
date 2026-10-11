using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Backups;
using TibiaHuntAnalyzer.Infrastructure.Backups;
using TibiaHuntAnalyzer.Infrastructure.Analytics;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Infrastructure.Sessions;
using TibiaHuntAnalyzer.Infrastructure.Settings;

namespace TibiaHuntAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var normalizedDatabasePath = Path.GetFullPath(databasePath);
        services.AddDbContextFactory<HuntAnalyzerDbContext>(options =>
            options.UseSqlite($"Data Source={normalizedDatabasePath};Pooling=False"));
        services.AddSingleton<IValidator<SaveSettingsRequest>, SaveSettingsRequestValidator>();
        services.AddSingleton<IValidator<SaveHuntSessionRequest>, SaveHuntSessionRequestValidator>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDatabasePathSettingsService>(
            _ => new DatabasePathSettingsService(
                normalizedDatabasePath,
                ApplicationDataPaths.GetConfigurationFilePath()));
        services.AddSingleton<IHuntSessionService, HuntSessionService>();
        services.AddSingleton<IHuntAnalyticsService, HuntAnalyticsService>();
        services.AddSingleton<IDatabaseBackupService>(
            _ => new DatabaseBackupService(normalizedDatabasePath));

        return services;
    }
}
