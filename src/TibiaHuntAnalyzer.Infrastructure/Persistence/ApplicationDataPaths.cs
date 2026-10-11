namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public static class ApplicationDataPaths
{
    private const string ApplicationDirectoryName = "TibiaHuntAnalyzer";
    private const string DatabaseFileName = "tibia-hunt-analyzer.db";
    private const string ConfigurationFileName = "database-path.json";

    public static string GetDatabasePath()
    {
        var configurationPath = GetConfigurationFilePath();
        return ReadDatabasePath(configurationPath, GetDefaultDatabasePath());
    }

    public static string GetDefaultDatabasePath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The per-user application data directory could not be resolved.");
        }

        var applicationDirectory = Path.Combine(localApplicationData, ApplicationDirectoryName);
        Directory.CreateDirectory(applicationDirectory);
        return Path.Combine(applicationDirectory, DatabaseFileName);
    }

    public static string GetConfigurationFilePath()
    {
        var defaultDatabasePath = GetDefaultDatabasePath();
        return Path.Combine(Path.GetDirectoryName(defaultDatabasePath)!, ConfigurationFileName);
    }

    public static async Task SaveDatabasePathAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var configurationPath = GetConfigurationFilePath();
        await SaveDatabasePathAsync(configurationPath, databasePath, cancellationToken);
    }

    public static string ReadDatabasePath(string configurationPath, string fallbackPath)
    {
        if (!File.Exists(configurationPath))
        {
            return Path.GetFullPath(fallbackPath);
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configurationPath));
            if (document.RootElement.TryGetProperty("DatabasePath", out var value) &&
                value.ValueKind == System.Text.Json.JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return Path.GetFullPath(value.GetString()!);
            }
        }
        catch (Exception exception) when (
            exception is System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidDataException(
                $"The database path configuration file '{configurationPath}' is unreadable or invalid.",
                exception);
        }

        throw new InvalidDataException(
            $"The database path configuration file '{configurationPath}' does not contain a valid database path.");
    }

    public static async Task SaveDatabasePathAsync(
        string configurationPath,
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(configurationPath)!);
        var temporaryPath = $"{configurationPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await System.Text.Json.JsonSerializer.SerializeAsync(
                    stream,
                    new DatabasePathConfiguration(Path.GetFullPath(databasePath)),
                    cancellationToken: cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, configurationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record DatabasePathConfiguration(string DatabasePath);
}
