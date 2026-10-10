using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class HuntSessionTests
{
    [Fact]
    public void Constructor_PreservesReportedDurationAndElapsedTimeSeparately()
    {
        var start = new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);
        var end = start.AddHours(1);

        var session = CreateSession(
            sessionStart: start,
            sessionEnd: end,
            duration: TimeSpan.FromMinutes(45));

        Assert.Equal(TimeSpan.FromMinutes(45), session.Duration);
        Assert.Equal(TimeSpan.FromHours(1), session.ElapsedTime);
    }

    [Fact]
    public void Constructor_AllowsZeroDurationAndEmptyRecordLists()
    {
        var start = new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);

        var session = CreateSession(
            sessionStart: start,
            sessionEnd: start,
            duration: TimeSpan.Zero);

        Assert.Empty(session.KilledMonsters);
        Assert.Empty(session.LootedItems);
        Assert.Equal(TimeSpan.Zero, session.ElapsedTime);
    }

    [Fact]
    public void Constructor_AssociatesMonstersAndItemsWithSession()
    {
        var monster = new KilledMonster("  Dragon ", 2);
        var item = new LootedItem("  Dragon Ham ", 3);

        var session = CreateSession(killedMonsters: [monster], lootedItems: [item]);

        Assert.Equal("Dragon", monster.Name);
        Assert.Equal("Dragon Ham", item.Name);
        Assert.Equal(session.Id, monster.HuntSessionId);
        Assert.Equal(session.Id, item.HuntSessionId);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveCharacterLevel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSession(characterLevel: 0));
    }

    [Fact]
    public void Constructor_RejectsEndBeforeStart()
    {
        var start = new DateTimeOffset(2026, 10, 10, 11, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => CreateSession(
            sessionStart: start,
            sessionEnd: start.AddMinutes(-1)));
    }

    [Fact]
    public void Constructor_RejectsNegativeDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSession(duration: TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void Constructor_RejectsRecordsAlreadyAssociatedWithAnotherSession()
    {
        var monster = new KilledMonster("Dragon", 1);
        _ = CreateSession(killedMonsters: [monster]);

        Assert.Throws<InvalidOperationException>(() => CreateSession(killedMonsters: [monster]));
    }

    [Fact]
    public void Constructor_DoesNotPartiallyAssociateRecordsWhenAnyRecordIsAlreadyAttached()
    {
        var attachedMonster = new KilledMonster("Dragon", 1);
        _ = CreateSession(killedMonsters: [attachedMonster]);

        var unattachedMonster = new KilledMonster("Frost Dragon", 1);

        Assert.Throws<InvalidOperationException>(() =>
            CreateSession(killedMonsters: [unattachedMonster, attachedMonster]));
        Assert.Equal(Guid.Empty, unattachedMonster.HuntSessionId);
    }

    [Fact]
    public void Constructor_RejectsDuplicateRecordInstanceWithoutAttachingIt()
    {
        var monster = new KilledMonster("Dragon", 1);

        Assert.Throws<ArgumentException>(() =>
            CreateSession(killedMonsters: [monster, monster]));
        Assert.Equal(Guid.Empty, monster.HuntSessionId);
    }

    private static HuntSession CreateSession(
        int characterLevel = 100,
        DateTimeOffset? sessionStart = null,
        DateTimeOffset? sessionEnd = null,
        TimeSpan? duration = null,
        IEnumerable<KilledMonster>? killedMonsters = null,
        IEnumerable<LootedItem>? lootedItems = null)
    {
        var start = sessionStart ?? new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);

        return new HuntSession(
            new Character("Knight"),
            new HuntLocation("Banuta"),
            characterLevel,
            start,
            sessionEnd ?? start.AddHours(1),
            duration ?? TimeSpan.FromHours(1),
            new HuntStatistics(),
            killedMonsters,
            lootedItems);
    }
}
