using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Application.Backups;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Backups;

public sealed class DatabaseBackupService(string databasePath) : IDatabaseBackupService
{
    public async Task CreateBackupAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var backupPath = CreateTemporaryPath();
        try
        {
            await BackupDatabaseToFileAsync(databasePath, backupPath, cancellationToken);
            await using var backupStream = new FileStream(
                backupPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
            await backupStream.CopyToAsync(destination, cancellationToken);
            await destination.FlushAsync(cancellationToken);
        }
        finally
        {
            File.Delete(backupPath);
        }
    }

    public async Task<string> RestoreBackupAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var stagedPath = CreateTemporaryPath();
        try
        {
            await using (var stagedStream = new FileStream(
                stagedPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            {
                await source.CopyToAsync(stagedStream, cancellationToken);
                await stagedStream.FlushAsync(cancellationToken);
            }

            await ValidateAndMigrateBackupAsync(stagedPath, cancellationToken);

            var safetyBackupPath = Path.Combine(
                Path.GetDirectoryName(databasePath)!,
                $"tibia-hunt-analyzer-pre-restore-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.db");
            await BackupDatabaseToFileAsync(databasePath, safetyBackupPath, cancellationToken);

            SqliteConnection.ClearAllPools();
            try
            {
                await CopyDatabaseAsync(stagedPath, databasePath, cancellationToken);
            }
            catch (Exception restoreException)
            {
                try
                {
                    await CopyDatabaseAsync(safetyBackupPath, databasePath, CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        $"Restore failed and the automatic rollback also failed. The safety backup is available at '{safetyBackupPath}'.",
                        restoreException,
                        rollbackException);
                }

                throw new InvalidOperationException(
                    $"Restore failed. The existing database was recovered from '{safetyBackupPath}'.",
                    restoreException);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
            }

            return safetyBackupPath;
        }
        finally
        {
            File.Delete(stagedPath);
        }
    }

    private static async Task ValidateAndMigrateBackupAsync(
        string backupPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = CreateConnectionString(backupPath);
            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                var integrity = await connection.CreateCommandWithText("PRAGMA integrity_check;")
                    .ExecuteScalarAsync(cancellationToken);
                if (!string.Equals(integrity as string, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("The selected file is not a valid, consistent SQLite database.");
                }

                var tables = new HashSet<string>(StringComparer.Ordinal);
                await using var tableCommand = connection.CreateCommandWithText(
                    "SELECT name FROM sqlite_master WHERE type = 'table';");
                await using var reader = await tableCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    tables.Add(reader.GetString(0));
                }

                if (!tables.Contains("__EFMigrationsHistory") ||
                    !tables.Contains("Characters") ||
                    !tables.Contains("HuntLocations"))
                {
                    throw new InvalidDataException(
                        "The selected SQLite database does not contain Tibia Hunt Analyzer data.");
                }
            }

            var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite(connectionString)
                .Options;
            await using var context = new HuntAnalyzerDbContext(options);
            await context.Database.MigrateAsync(cancellationToken);

            await using var validationConnection = new SqliteConnection(connectionString);
            await validationConnection.OpenAsync(cancellationToken);
            await using var foreignKeyCommand = validationConnection.CreateCommandWithText("PRAGMA foreign_key_check;");
            await using var foreignKeyReader = await foreignKeyCommand.ExecuteReaderAsync(cancellationToken);
            if (await foreignKeyReader.ReadAsync(cancellationToken))
            {
                throw new InvalidDataException("The selected database contains broken relationships.");
            }
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException(
                "The selected file is not a valid, readable Tibia Hunt Analyzer SQLite backup.",
                exception);
        }
    }

    private static async Task BackupDatabaseToFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var source = new SqliteConnection(CreateConnectionString(sourcePath, readOnly: true));
        await using var destination = new SqliteConnection(CreateConnectionString(destinationPath));
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        await Task.Run(() => source.BackupDatabase(destination), cancellationToken);
    }

    private static async Task CopyDatabaseAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var source = new SqliteConnection(CreateConnectionString(sourcePath, readOnly: true));
        await using var destination = new SqliteConnection(CreateConnectionString(destinationPath));
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        await Task.Run(() => source.BackupDatabase(destination), cancellationToken);
    }

    private static string CreateConnectionString(string path, bool readOnly = false) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();

    private static string CreateTemporaryPath() =>
        Path.Combine(Path.GetTempPath(), $"tibia-hunt-analyzer-{Guid.NewGuid():N}.db");
}

internal static class SqliteCommandExtensions
{
    public static SqliteCommand CreateCommandWithText(this SqliteConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command;
    }
}
