using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Backups;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Tests.Integration;

public sealed class DatabaseBackupServiceTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"tibia-hunt-backup-{Guid.NewGuid():N}");
    private string _databasePath = string.Empty;
    private Guid _originalCharacterId;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "app.db");
        await using var context = CreateContext(_databasePath);
        await context.Database.MigrateAsync();
        var character = new Character("Backup Knight");
        context.Characters.Add(character);
        await context.SaveChangesAsync();
        _originalCharacterId = character.Id;
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
    public async Task RestoreBackupAsync_RestoresDataAndPreservesSafetyBackup()
    {
        var service = new DatabaseBackupService(_databasePath);
        await using var backup = new MemoryStream();
        await service.CreateBackupAsync(backup);

        await using (var context = CreateContext(_databasePath))
        {
            context.Characters.Add(new Character("Added After Backup"));
            await context.SaveChangesAsync();
        }

        backup.Position = 0;
        var safetyBackupPath = await service.RestoreBackupAsync(backup);

        Assert.True(File.Exists(safetyBackupPath));
        await using var restored = CreateContext(_databasePath);
        var characters = await restored.Characters.AsNoTracking().ToListAsync();
        var character = Assert.Single(characters);
        Assert.Equal(_originalCharacterId, character.Id);
        Assert.Equal("Backup Knight", character.Name);

        await using var safetyContext = CreateContext(safetyBackupPath);
        Assert.Equal(2, await safetyContext.Characters.CountAsync());
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsNonDatabaseWithoutChangingCurrentData()
    {
        var service = new DatabaseBackupService(_databasePath);
        await using var invalidBackup = new MemoryStream("not a sqlite database"u8.ToArray());

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.RestoreBackupAsync(invalidBackup));

        await using var context = CreateContext(_databasePath);
        var character = await context.Characters.AsNoTracking().SingleAsync();
        Assert.Equal(_originalCharacterId, character.Id);
    }

    private static HuntAnalyzerDbContext CreateContext(string path)
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite($"Data Source={path};Pooling=False")
            .Options;
        return new HuntAnalyzerDbContext(options);
    }
}
