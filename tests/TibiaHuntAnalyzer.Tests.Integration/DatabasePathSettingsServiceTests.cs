using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Infrastructure.Settings;

namespace TibiaHuntAnalyzer.Tests.Integration;

public sealed class DatabasePathSettingsServiceTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"tibia-hunt-path-settings-{Guid.NewGuid():N}");
    private string _activeDatabasePath = string.Empty;
    private string _configurationPath = string.Empty;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _activeDatabasePath = Path.Combine(_directory, "active.db");
        _configurationPath = Path.Combine(_directory, "database-path.json");
        await using var activeDatabase = CreateContext(_activeDatabasePath);
        await activeDatabase.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ConfigureDatabasePathAsync_InitializesNewDatabaseAndPersistsConfiguration()
    {
        var selectedPath = Path.Combine(_directory, "selected.sqlite");
        var service = CreateService();

        await service.ConfigureDatabasePathAsync(selectedPath);

        Assert.True(File.Exists(selectedPath));
        Assert.Equal(
            Path.GetFullPath(selectedPath),
            await service.GetConfiguredDatabasePathAsync());
        Assert.Equal(Path.GetFullPath(_activeDatabasePath), service.ActiveDatabasePath);

        await using var selectedDatabase = CreateContext(selectedPath);
        Assert.True(await selectedDatabase.Database.CanConnectAsync());

        var reloadedService = CreateService();
        Assert.Equal(Path.GetFullPath(selectedPath), await reloadedService.GetConfiguredDatabasePathAsync());
    }

    [Fact]
    public async Task ConfigureDatabasePathAsync_RejectsIncompatibleDatabaseAndKeepsConfigurationUnchanged()
    {
        var incompatiblePath = Path.Combine(_directory, "other.sqlite");
        await File.WriteAllTextAsync(incompatiblePath, "not an application database");
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ConfigureDatabasePathAsync(incompatiblePath));

        Assert.False(File.Exists(_configurationPath));
        Assert.Equal(
            Path.GetFullPath(_activeDatabasePath),
            await service.GetConfiguredDatabasePathAsync());
    }

    [Fact]
    public async Task ConfigureDatabasePathAsync_FailedChangeKeepsPreviouslySavedPath()
    {
        var service = CreateService();
        var selectedPath = Path.Combine(_directory, "selected.db");
        await service.ConfigureDatabasePathAsync(selectedPath);
        var incompatiblePath = Path.Combine(_directory, "incompatible.db");
        await File.WriteAllTextAsync(incompatiblePath, "not an application database");

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ConfigureDatabasePathAsync(incompatiblePath));

        Assert.Equal(
            Path.GetFullPath(selectedPath),
            await service.GetConfiguredDatabasePathAsync());
    }

    [Fact]
    public async Task ConfigureDatabasePathAsync_DoesNotInitializeOrReplaceAnExistingEmptyFile()
    {
        var selectedPath = Path.Combine(_directory, "existing-empty.db");
        await File.WriteAllBytesAsync(selectedPath, []);
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ConfigureDatabasePathAsync(selectedPath));

        Assert.Empty(await File.ReadAllBytesAsync(selectedPath));
        Assert.False(File.Exists(_configurationPath));
    }

    [Fact]
    public async Task ConfigureDatabasePathAsync_RejectsMissingDirectoryWithoutChangingConfiguration()
    {
        var invalidPath = Path.Combine(_directory, "missing", "selected.db");
        var service = CreateService();

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => service.ConfigureDatabasePathAsync(invalidPath));

        Assert.False(File.Exists(_configurationPath));
    }

    [Fact]
    public async Task Startup_CreatesMissingConfiguredDatabase()
    {
        var missingPath = Path.Combine(_directory, "deleted.db");

        await DatabasePathCompatibility.InitializeConfiguredDatabaseAsync(missingPath);

        Assert.True(File.Exists(missingPath));
        await DatabasePathCompatibility.ValidateExistingDatabaseAsync(missingPath);
    }

    private DatabasePathSettingsService CreateService() =>
        new(_activeDatabasePath, _configurationPath);

    private static HuntAnalyzerDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;
        return new HuntAnalyzerDbContext(options);
    }
}
