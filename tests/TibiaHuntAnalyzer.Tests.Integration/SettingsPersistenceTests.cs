using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Tests.Integration;

public sealed class SettingsPersistenceTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"tibia-hunt-analyzer-{Guid.NewGuid():N}.db");

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Settings_ArePersistedAndCanBeDeactivatedWithoutChangingIdentity()
    {
        var character = new Character("  Test Knight ");
        var location = new HuntLocation("  Test Cave ");

        await using (var context = CreateContext())
        {
            context.Characters.Add(character);
            context.HuntLocations.Add(location);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedCharacter = await context.Characters.SingleAsync();
            var savedLocation = await context.HuntLocations.SingleAsync();

            Assert.Equal(character.Id, savedCharacter.Id);
            Assert.Equal("Test Knight", savedCharacter.Name);
            Assert.True(savedCharacter.IsActive);
            Assert.Equal(location.Id, savedLocation.Id);
            Assert.Equal("Test Cave", savedLocation.Name);
            Assert.True(savedLocation.IsActive);

            savedCharacter.Deactivate();
            savedLocation.Deactivate();
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var savedCharacter = await context.Characters.SingleAsync();
            var savedLocation = await context.HuntLocations.SingleAsync();

            Assert.Equal(character.Id, savedCharacter.Id);
            Assert.False(savedCharacter.IsActive);
            Assert.Equal(location.Id, savedLocation.Id);
            Assert.False(savedLocation.IsActive);
        }
    }

    [Fact]
    public async Task DuplicateNames_AreNotSilentlyRejected()
    {
        await using var context = CreateContext();
        context.Characters.AddRange(new Character("Knight"), new Character("Knight"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Characters.CountAsync());
    }

    private HuntAnalyzerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite($"Data Source={_databasePath};Pooling=False")
            .Options;

        return new HuntAnalyzerDbContext(options);
    }
}
