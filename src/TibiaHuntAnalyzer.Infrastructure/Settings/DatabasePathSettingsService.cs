using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Settings;

public sealed class DatabasePathSettingsService(
    string activeDatabasePath,
    string configurationPath) : IDatabasePathSettingsService
{
    public string ActiveDatabasePath { get; } = Path.GetFullPath(activeDatabasePath);

    public Task<string> GetConfiguredDatabasePathAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ApplicationDataPaths.ReadDatabasePath(configurationPath, ActiveDatabasePath));
    }

    public async Task ConfigureDatabasePathAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var normalizedPath = Path.GetFullPath(databasePath.Trim());
        if (string.Equals(
            normalizedPath,
            configurationPath,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            throw new InvalidDataException("The database file cannot be the application configuration file.");
        }

        if (File.Exists(normalizedPath))
        {
            await DatabasePathCompatibility.ValidateExistingDatabaseAsync(normalizedPath, cancellationToken);
        }
        else
        {
            await DatabasePathCompatibility.InitializeNewDatabaseAsync(normalizedPath, cancellationToken);
        }

        await ApplicationDataPaths.SaveDatabasePathAsync(
            configurationPath,
            normalizedPath,
            cancellationToken);
    }
}
