namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public static class ApplicationDataPaths
{
    public static string GetDatabasePath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The per-user application data directory could not be resolved.");
        }

        var applicationDirectory = Path.Combine(localApplicationData, "TibiaHuntAnalyzer");
        Directory.CreateDirectory(applicationDirectory);
        return Path.Combine(applicationDirectory, "tibia-hunt-analyzer.db");
    }
}
