namespace TibiaHuntAnalyzer.Application.Settings;

public interface IDatabasePathSettingsService
{
    string ActiveDatabasePath { get; }

    Task<string> GetConfiguredDatabasePathAsync(CancellationToken cancellationToken = default);

    Task ConfigureDatabasePathAsync(string databasePath, CancellationToken cancellationToken = default);
}
