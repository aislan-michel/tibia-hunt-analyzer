using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TibiaHuntAnalyzer.Infrastructure.Persistence;

public static class DatabasePathCompatibility
{
    private static readonly HashSet<string> ReviewedMigrations =
    [
        "20261010211255_InitialSettings",
        "20261010212820_AddHuntSessions"
    ];

    public static async Task ValidateExistingDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(databasePath))
        {
            throw new FileNotFoundException("The configured database file does not exist.", databasePath);
        }

        try
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ToString());
            await connection.OpenAsync(cancellationToken);

            var integrity = await ExecuteScalarAsync(connection, "PRAGMA integrity_check;", cancellationToken);
            if (!string.Equals(integrity as string, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The selected SQLite database failed its integrity check.");
            }

            var tables = await ReadTableNamesAsync(connection, cancellationToken);
            if (!tables.Contains("__EFMigrationsHistory") ||
                !tables.Contains("Characters") ||
                !tables.Contains("HuntLocations"))
            {
                throw new InvalidDataException(
                    "The selected database is not a compatible Tibia Hunt Analyzer database.");
            }

            var appliedMigrations = await ReadAppliedMigrationsAsync(connection, cancellationToken);
            if (!appliedMigrations.Contains("20261010211255_InitialSettings") ||
                appliedMigrations.Any(id => !ReviewedMigrations.Contains(id)))
            {
                throw new InvalidDataException(
                    "The selected database has an unknown or newer migration history and cannot be used safely.");
            }

            await RequireColumnsAsync(
                connection,
                "Characters",
                ["Id", "Name", "IsActive"],
                cancellationToken);
            await RequireColumnsAsync(
                connection,
                "HuntLocations",
                ["Id", "Name", "IsActive"],
                cancellationToken);

            if (appliedMigrations.Contains("20261010212820_AddHuntSessions") &&
                (!tables.Contains("HuntSessions") ||
                 !tables.Contains("KilledMonsters") ||
                 !tables.Contains("LootedItems")))
            {
                throw new InvalidDataException(
                    "The selected database is missing required session tables.");
            }

            if (appliedMigrations.Contains("20261010212820_AddHuntSessions"))
            {
                await RequireColumnsAsync(
                    connection,
                    "HuntSessions",
                    [
                        "Id", "CharacterId", "HuntLocationId", "CharacterLevel",
                        "SessionStart", "SessionEnd", "Duration",
                        "Statistics_Balance", "Statistics_Damage", "Statistics_DamagePerHour",
                        "Statistics_Healing", "Statistics_HealingPerHour", "Statistics_Loot",
                        "Statistics_Supplies", "Statistics_XpGain", "Statistics_XpPerHour",
                        "Statistics_RawXpGain", "Statistics_RawXpPerHour",
                        "InputFormat", "OriginalInput"
                    ],
                    cancellationToken);
                await RequireColumnsAsync(
                    connection,
                    "KilledMonsters",
                    ["Id", "HuntSessionId", "Name", "Count"],
                    cancellationToken);
                await RequireColumnsAsync(
                    connection,
                    "LootedItems",
                    ["Id", "HuntSessionId", "Name", "Quantity"],
                    cancellationToken);
            }

            await using var foreignKeyCommand = connection.CreateCommand();
            foreignKeyCommand.CommandText = "PRAGMA foreign_key_check;";
            await using var foreignKeyReader = await foreignKeyCommand.ExecuteReaderAsync(cancellationToken);
            if (await foreignKeyReader.ReadAsync(cancellationToken))
            {
                throw new InvalidDataException("The selected database contains broken relationships.");
            }
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException(
                "The selected file is not a readable Tibia Hunt Analyzer SQLite database.",
                exception);
        }
    }

    public static async Task InitializeNewDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidDataException("The database path must include a parent directory.");
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The database directory '{directory}' does not exist.");
        }

        var probePath = Path.Combine(directory, $".tha-write-test-{Guid.NewGuid():N}");
        try
        {
            await using (new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }
        }
        finally
        {
            if (File.Exists(probePath))
            {
                File.Delete(probePath);
            }
        }

        if (File.Exists(databasePath))
        {
            throw new IOException("The database file already exists; it will not be overwritten.");
        }

        var stagedPath = Path.Combine(directory, $".tha-new-database-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite(new SqliteConnectionStringBuilder
                {
                    DataSource = stagedPath,
                    Pooling = false
                }.ToString())
                .Options;
            await using (var context = new HuntAnalyzerDbContext(options))
            {
                await context.Database.MigrateAsync(cancellationToken);
            }

            await ValidateExistingDatabaseAsync(stagedPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagedPath, databasePath);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            DeleteIfPresent(stagedPath);
            DeleteIfPresent($"{stagedPath}-shm");
            DeleteIfPresent($"{stagedPath}-wal");
        }
    }

    public static async Task InitializeConfiguredDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(databasePath))
        {
            await InitializeNewDatabaseAsync(databasePath, cancellationToken);
            return;
        }

        await ValidateExistingDatabaseAsync(databasePath, cancellationToken);
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false
            }.ToString())
            .Options;
        await using var context = new HuntAnalyzerDbContext(options);
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any(id => !ReviewedMigrations.Contains(id)))
        {
            throw new InvalidDataException(
                "The configured database requires a migration that has not been reviewed for safe application.");
        }

        await context.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> ReadTableNamesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var tables = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task<HashSet<string>> ReadAppliedMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var migrations = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            migrations.Add(reader.GetString(0));
        }

        return migrations;
    }

    private static async Task RequireColumnsAsync(
        SqliteConnection connection,
        string table,
        IReadOnlyCollection<string> requiredColumns,
        CancellationToken cancellationToken)
    {
        var actualColumns = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            actualColumns.Add(reader.GetString(1));
        }

        if (requiredColumns.Any(column => !actualColumns.Contains(column)))
        {
            throw new InvalidDataException(
                $"The selected application database table '{table}' does not match the expected schema.");
        }
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
