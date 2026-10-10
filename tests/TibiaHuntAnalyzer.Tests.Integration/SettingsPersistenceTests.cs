using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Infrastructure.Settings;

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

    [Fact]
    public async Task SettingsService_SavesNewAndEditedSettingsAndReportsValidationFailures()
    {
        var factory = new PooledDbContextFactory<HuntAnalyzerDbContext>(
            new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite($"Data Source={_databasePath};Pooling=False")
                .Options);
        var service = new SettingsService(factory, new SaveSettingsRequestValidator());

        await service.SaveAsync(new SaveSettingsRequest(
            [new SettingItem(null, "Knight", true)],
            [new SettingItem(null, "Banuta", true)]));

        var character = Assert.Single(await service.GetCharactersAsync());
        var location = Assert.Single(await service.GetHuntLocationsAsync());

        await service.SaveAsync(new SaveSettingsRequest(
            [character with { Name = "Elite Knight", IsActive = false }],
            [location with { Name = "Deeper Banuta" }]));

        var updatedCharacter = Assert.Single(await service.GetCharactersAsync());
        var updatedLocation = Assert.Single(await service.GetHuntLocationsAsync());
        Assert.Equal(character.Id, updatedCharacter.Id);
        Assert.Equal("Elite Knight", updatedCharacter.Name);
        Assert.False(updatedCharacter.IsActive);
        Assert.Equal(location.Id, updatedLocation.Id);
        Assert.Equal("Deeper Banuta", updatedLocation.Name);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.SaveAsync(new SaveSettingsRequest(
                [updatedCharacter with { Name = " " }],
                [updatedLocation])));

        Assert.Equal(updatedCharacter, Assert.Single(await service.GetCharactersAsync()));
        Assert.Equal(updatedLocation, Assert.Single(await service.GetHuntLocationsAsync()));
    }

    private HuntAnalyzerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite($"Data Source={_databasePath};Pooling=False")
            .Options;

        return new HuntAnalyzerDbContext(options);
    }
}
