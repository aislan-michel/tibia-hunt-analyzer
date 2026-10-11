using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Persistence;
using TibiaHuntAnalyzer.Infrastructure.Analytics;
using TibiaHuntAnalyzer.Infrastructure.Sessions;
using TibiaHuntAnalyzer.Infrastructure.Settings;

namespace TibiaHuntAnalyzer.Tests.Integration;

public sealed class HuntSessionPersistenceTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"tibia-hunt-sessions-{Guid.NewGuid():N}.db");

    private Guid _characterId;
    private Guid _huntLocationId;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var character = new Character("Test Knight");
        var location = new HuntLocation("Test Cave");
        context.Characters.Add(character);
        context.HuntLocations.Add(location);
        await context.SaveChangesAsync();

        _characterId = character.Id;
        _huntLocationId = location.Id;
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
    public async Task SaveAndGetDetails_PersistsCompleteSessionAsRelationalData()
    {
        var service = CreateService();
        var parsed = ParseSession(
            """
            {
              "Balance": "-125.5",
              "Damage": "12000",
              "Damage/h": "24000",
              "Healing": "3500",
              "Healing/h": "7000",
              "Killed Monsters": [{"Name": "Dragon", "Killed": "2"}],
              "Loot": "500",
              "Looted Items": [{"Name": "Dragon Ham", "Amount": 3}],
              "Raw XP Gain": "9000",
              "Raw XP/h": "18000",
              "Session end": "2026-10-10T11:00:00Z",
              "Session length": "00:45:00",
              "Session start": "2026-10-10T10:00:00Z",
              "Supplies": "625.5",
              "XP Gain": "8000",
              "XP/h": "16000"
            }
            """);

        var sessionId = await service.SaveAsync(
            new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, parsed));

        var details = await service.GetDetailsAsync(sessionId);

        Assert.NotNull(details);
        Assert.Equal(_characterId, details.CharacterId);
        Assert.Equal("Test Knight", details.CharacterName);
        Assert.Equal(150, details.CharacterLevel);
        Assert.Equal(_huntLocationId, details.HuntLocationId);
        Assert.Equal("Test Cave", details.HuntLocationName);
        Assert.Equal(TimeSpan.FromMinutes(45), details.Duration);
        Assert.Equal(TimeSpan.FromHours(1), details.ElapsedTime);
        Assert.Equal(SessionInputFormat.Json, details.InputFormat);
        Assert.Equal(parsed.OriginalContent, details.OriginalInput);
        Assert.Equal(-125.5m, details.Statistics.Balance);
        Assert.Equal(8000, details.Statistics.XpGain);
        Assert.Equal(9000, details.Statistics.RawXpGain);
        Assert.Collection(details.KilledMonsters, monster =>
        {
            Assert.Equal("Dragon", monster.Name);
            Assert.Equal(2, monster.Count);
        });
        Assert.Collection(details.LootedItems, item =>
        {
            Assert.Equal("Dragon Ham", item.Name);
            Assert.Equal(3, item.Count);
        });

        await using var context = CreateContext();
        Assert.Equal(1, await context.HuntSessions.CountAsync());
        Assert.Equal(1, await context.KilledMonsters.CountAsync());
        Assert.Equal(1, await context.LootedItems.CountAsync());

        var summary = Assert.Single(await service.GetSummariesAsync());
        Assert.Equal(sessionId, summary.Id);
        Assert.Equal("Test Knight", summary.CharacterName);
        Assert.Equal(150, summary.CharacterLevel);
        Assert.Equal("Test Cave", summary.HuntLocationName);
        Assert.Equal(details.SessionStart, summary.SessionStart);
    }

    [Fact]
    public async Task SaveAsync_AllowsDuplicateSessions()
    {
        var service = CreateService();
        var parsed = ParseSession(
            """
            {
              "Session end": "2026-10-10T11:00:00Z",
              "Session length": "00:45:00",
              "Session start": "2026-10-10T10:00:00Z"
            }
            """);
        var request = new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, parsed);

        var firstId = await service.SaveAsync(request);
        var secondId = await service.SaveAsync(request);

        Assert.NotEqual(firstId, secondId);
        await using var context = CreateContext();
        Assert.Equal(2, await context.HuntSessions.CountAsync());
        Assert.Equal(2, (await service.GetSummariesAsync()).Count);
    }

    [Fact]
    public async Task SaveAsync_RejectsMissingRequiredParsedFieldsWithoutPersistence()
    {
        var service = CreateService();
        var parsed = new ParsedHuntSession(
            HuntInputFormat.Json,
            """{"XP Gain":1}""",
            null,
            null,
            null,
            new HuntStatistics(xpGain: 1),
            [],
            []);

        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(
            new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, parsed)));

        await using var context = CreateContext();
        Assert.Empty(await context.HuntSessions.ToListAsync());
    }

    [Fact]
    public async Task SaveAsync_RollsBackSessionStatisticsAndChildrenWhenPersistenceFails()
    {
        await using (var context = CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER RejectLootedItems
                BEFORE INSERT ON LootedItems
                BEGIN
                    SELECT RAISE(ABORT, 'forced looted item failure');
                END;
                """);
        }

        var service = CreateService();
        var parsed = ParseSession(
            """
            {
              "Balance": "-10",
              "Killed Monsters": [{"Name": "Dragon", "Killed": "1"}],
              "Looted Items": [{"Name": "Dragon Ham", "Amount": 1}],
              "Session end": "2026-10-10T11:00:00Z",
              "Session length": "00:45:00",
              "Session start": "2026-10-10T10:00:00Z"
            }
            """);

        var request = new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, parsed);
        await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAsync(request));

        await using var verify = CreateContext();
        Assert.Empty(await verify.HuntSessions.ToListAsync());
        Assert.Empty(await verify.KilledMonsters.ToListAsync());
        Assert.Empty(await verify.LootedItems.ToListAsync());

        await verify.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectLootedItems;");
        await service.SaveAsync(request);

        Assert.Single(await verify.HuntSessions.ToListAsync());
        Assert.Single(await verify.KilledMonsters.ToListAsync());
        Assert.Single(await verify.LootedItems.ToListAsync());
    }

    [Fact]
    public async Task AnalyticsService_LoadsPersistedMetricsAndAppliesFiltersAndGroups()
    {
        var sessionService = CreateService();
        var first = ParseSession(
            """
            {
              "Balance": "100",
              "Session end": "2026-10-10T11:00:00Z",
              "Session length": "01:00:00",
              "Session start": "2026-10-10T10:00:00Z",
              "XP Gain": "1800"
            }
            """);
        var second = ParseSession(
            """
            {
              "Balance": "-40",
              "Session end": "2026-10-11T12:00:00Z",
              "Session length": "02:00:00",
              "Session start": "2026-10-11T10:00:00Z",
              "XP Gain": "2000"
            }
            """);

        await sessionService.SaveAsync(new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, first));
        await sessionService.SaveAsync(new SaveHuntSessionRequest(_characterId, _huntLocationId, 151, second));
        var analyticsService = CreateAnalyticsService();

        var allSessions = await analyticsService.GetReportAsync();
        var filtered = await analyticsService.GetReportAsync(new HuntAnalyticsFilter(
            CharacterId: _characterId,
            HuntLocationId: _huntLocationId,
            StartDate: new DateOnly(2026, 10, 11),
            EndDate: new DateOnly(2026, 10, 11)));

        Assert.Equal(2, allSessions.Overall.SessionCount);
        Assert.Equal(60m, allSessions.Overall.Balance.Total);
        Assert.Equal(30m, allSessions.Overall.Balance.Average);
        Assert.Equal(2, allSessions.Overall.XpGain.ContributingSessionCount);
        Assert.Equal(3800m, allSessions.Overall.XpGain.Total);
        Assert.Equal(2, Assert.Single(allSessions.ByCharacter).Metrics.SessionCount);
        Assert.Equal(2, Assert.Single(allSessions.ByHuntLocation).Metrics.SessionCount);
        Assert.Equal(2, allSessions.LevelProgression.Count);

        Assert.Single(filtered.Sessions);
        Assert.Equal(151, filtered.Sessions[0].CharacterLevel);
        Assert.Equal(-40m, filtered.Overall.Balance.Total);
        Assert.Equal(1000m, filtered.Sessions[0].CalculatedXpPerHour);
    }

    [Fact]
    public async Task EditingAndDeactivatingSettings_PreservesHistoricalSessionAssociations()
    {
        var sessionService = CreateService();
        var parsed = ParseSession(
            """{"Session end":"2026-10-10T11:00:00Z","Session length":"00:45:00","Session start":"2026-10-10T10:00:00Z"}""");
        var sessionId = await sessionService.SaveAsync(
            new SaveHuntSessionRequest(_characterId, _huntLocationId, 150, parsed));
        var settingsService = CreateSettingsService();
        var character = Assert.Single(await settingsService.GetCharactersAsync());
        var location = Assert.Single(await settingsService.GetHuntLocationsAsync());

        await settingsService.SaveAsync(new SaveSettingsRequest(
            [character with { Name = "Renamed Knight", IsActive = false }],
            []));
        await settingsService.SaveAsync(new SaveSettingsRequest(
            [],
            [location with { Name = "Renamed Cave", IsActive = false }]));

        var details = await sessionService.GetDetailsAsync(sessionId);
        Assert.NotNull(details);
        Assert.Equal(_characterId, details.CharacterId);
        Assert.Equal("Renamed Knight", details.CharacterName);
        Assert.Equal(_huntLocationId, details.HuntLocationId);
        Assert.Equal("Renamed Cave", details.HuntLocationName);
        Assert.Single(await sessionService.GetSummariesAsync());
    }

    private HuntSessionService CreateService()
    {
        var factory = new PooledDbContextFactory<HuntAnalyzerDbContext>(
            new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite($"Data Source={_databasePath};Pooling=False")
                .Options);

        return new HuntSessionService(factory, new SaveHuntSessionRequestValidator());
    }

    private HuntAnalyticsService CreateAnalyticsService()
    {
        var factory = new PooledDbContextFactory<HuntAnalyzerDbContext>(
            new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite($"Data Source={_databasePath};Pooling=False")
                .Options);

        return new HuntAnalyticsService(factory);
    }

    private SettingsService CreateSettingsService()
    {
        var factory = new PooledDbContextFactory<HuntAnalyzerDbContext>(
            new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
                .UseSqlite($"Data Source={_databasePath};Pooling=False")
                .Options);

        return new SettingsService(factory, new SaveSettingsRequestValidator());
    }

    private HuntAnalyzerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HuntAnalyzerDbContext>()
            .UseSqlite($"Data Source={_databasePath};Pooling=False")
            .Options;

        return new HuntAnalyzerDbContext(options);
    }

    private static ParsedHuntSession ParseSession(string json)
    {
        var result = new JsonHuntSessionParser().Parse(json);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Errors));
        return result.Session!;
    }
}
