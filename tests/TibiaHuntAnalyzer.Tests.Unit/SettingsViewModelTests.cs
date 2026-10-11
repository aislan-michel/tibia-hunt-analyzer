using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class SettingsViewModelTests
{
    [Fact]
    public async Task SaveCommand_ReportsSuccessAndRequiresRestartForDifferentDatabase()
    {
        var service = new FakeDatabasePathSettingsService("C:\\data\\current.db");
        var viewModel = new SettingsViewModel(service);
        await viewModel.LoadAsync();
        viewModel.DatabasePath = "C:\\data\\next.db";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(Path.GetFullPath("C:\\data\\next.db"), service.SavedPath);
        Assert.True(viewModel.RestartRequired);
        Assert.Contains("reopen", viewModel.StatusMessage);
        Assert.False(viewModel.HasUnsavedChanges);
    }

    [Fact]
    public async Task SaveCommand_LeavesSelectedPathAndCurrentConfigurationWhenValidationFails()
    {
        var service = new FakeDatabasePathSettingsService("C:\\data\\current.db")
        {
            SaveException = new InvalidDataException("Not an application database.")
        };
        var viewModel = new SettingsViewModel(service);
        await viewModel.LoadAsync();
        viewModel.DatabasePath = "C:\\data\\invalid.db";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("C:\\data\\invalid.db", viewModel.DatabasePath);
        Assert.Equal("C:\\data\\current.db", await service.GetConfiguredDatabasePathAsync());
        Assert.True(viewModel.HasUnsavedChanges);
        Assert.Contains("Not an application database.", viewModel.StatusMessage);
    }

    private sealed class FakeDatabasePathSettingsService(string activeDatabasePath) : IDatabasePathSettingsService
    {
        public string ActiveDatabasePath { get; } = activeDatabasePath;

        public string? SavedPath { get; private set; }

        public Exception? SaveException { get; init; }

        public Task<string> GetConfiguredDatabasePathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SavedPath ?? ActiveDatabasePath);

        public Task ConfigureDatabasePathAsync(
            string databasePath,
            CancellationToken cancellationToken = default)
        {
            if (SaveException is not null)
            {
                throw SaveException;
            }

            SavedPath = databasePath;
            return Task.CompletedTask;
        }
    }
}
