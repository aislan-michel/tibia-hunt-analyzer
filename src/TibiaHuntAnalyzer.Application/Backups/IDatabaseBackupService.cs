namespace TibiaHuntAnalyzer.Application.Backups;

public interface IDatabaseBackupService
{
    Task CreateBackupAsync(Stream destination, CancellationToken cancellationToken = default);

    Task<string> RestoreBackupAsync(Stream source, CancellationToken cancellationToken = default);
}
