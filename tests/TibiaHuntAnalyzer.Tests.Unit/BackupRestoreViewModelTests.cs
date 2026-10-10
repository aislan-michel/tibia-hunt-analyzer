using TibiaHuntAnalyzer.Application.Backups;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class BackupRestoreViewModelTests
{
    [Fact]
    public async Task RestoreBackupAsync_RequiresConfirmationBeforeReplacingDatabase()
    {
        var backupService = new FakeDatabaseBackupService();
        var viewModel = new BackupRestoreViewModel(backupService);

        await viewModel.RestoreBackupAsync(new MemoryStream());

        Assert.Equal(0, backupService.RestoreCallCount);
        Assert.Contains("Confirm", viewModel.StatusMessage);
    }

    [Fact]
    public async Task RestoreBackupAsync_ReportsSafetyBackupAndClearsConfirmation()
    {
        var backupService = new FakeDatabaseBackupService();
        var viewModel = new BackupRestoreViewModel(backupService)
        {
            ConfirmRestore = true
        };

        await viewModel.RestoreBackupAsync(new MemoryStream());

        Assert.Equal(1, backupService.RestoreCallCount);
        Assert.Contains("safety-copy.db", viewModel.StatusMessage);
        Assert.False(viewModel.ConfirmRestore);
    }

    private sealed class FakeDatabaseBackupService : IDatabaseBackupService
    {
        public int RestoreCallCount { get; private set; }

        public Task CreateBackupAsync(Stream destination, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> RestoreBackupAsync(Stream source, CancellationToken cancellationToken = default)
        {
            RestoreCallCount++;
            return Task.FromResult("safety-copy.db");
        }
    }
}
