using TibiaHuntAnalyzer.Application.Imports;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class TextHuntSessionParserTests
{
    private readonly TextHuntSessionParser _parser = new();

    [Fact]
    public void Parse_ConvertsSuppliedSessionFormatAndPreservesOriginalContent()
    {
        const string text = """
            Session data: From 2026-10-10, 18:03:54 to 2026-10-10, 18:04:59
            Session: 00:01h
            Raw XP Gain: 210
            XP Gain: 312
            Raw XP/h: 11,505
            XP/h: 17,094
            Loot: 2
            Supplies: 1,820
            Balance: -1,818
            Damage: 660
            Damage/h: 47,375
            Healing: 0
            Healing/h: 0
            Killed Monsters:
              2x bat
              16x cave rat
              6x rat
            Looted Items:
              2x a gold coin
            """;

        var result = _parser.Parse(text);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Session);
        Assert.Equal(HuntInputFormat.Text, result.Session.SourceFormat);
        Assert.Equal(text, result.Session.OriginalContent);
        Assert.Equal(
            new DateTimeOffset(
                2026,
                10,
                10,
                18,
                3,
                54,
                TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 10, 18, 3, 54))),
            result.Session.SessionStart);
        Assert.Equal(
            new DateTimeOffset(
                2026,
                10,
                10,
                18,
                4,
                59,
                TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 10, 18, 4, 59))),
            result.Session.SessionEnd);
        Assert.Equal(TimeSpan.FromMinutes(1), result.Session.Duration);
        Assert.Equal(TimeSpan.FromSeconds(65), result.Session.SessionEnd - result.Session.SessionStart);
        Assert.Equal(210, result.Session.Statistics.RawXpGain);
        Assert.Equal(312, result.Session.Statistics.XpGain);
        Assert.Equal(11505, result.Session.Statistics.RawXpPerHour);
        Assert.Equal(17094, result.Session.Statistics.XpPerHour);
        Assert.Equal(2, result.Session.Statistics.Loot);
        Assert.Equal(1820, result.Session.Statistics.Supplies);
        Assert.Equal(-1818, result.Session.Statistics.Balance);
        Assert.Equal(660, result.Session.Statistics.Damage);
        Assert.Equal(47375, result.Session.Statistics.DamagePerHour);
        Assert.Equal(0, result.Session.Statistics.Healing);
        Assert.Equal(0, result.Session.Statistics.HealingPerHour);
        Assert.Collection(
            result.Session.KilledMonsters,
            monster =>
            {
                Assert.Equal("bat", monster.Name);
                Assert.Equal(2, monster.Count);
            },
            monster =>
            {
                Assert.Equal("cave rat", monster.Name);
                Assert.Equal(16, monster.Count);
            },
            monster =>
            {
                Assert.Equal("rat", monster.Name);
                Assert.Equal(6, monster.Count);
            });
        Assert.Collection(
            result.Session.LootedItems,
            item =>
            {
                Assert.Equal("a gold coin", item.Name);
                Assert.Equal(2, item.Quantity);
            });
    }

    [Theory]
    [InlineData("Killed Monsters: None\nLooted Items: None")]
    [InlineData("Killed Monsters:\nLooted Items:")]
    public void Parse_HandlesEmptyAndNoneLists(string lists)
    {
        var result = _parser.Parse(lists);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Session!.KilledMonsters);
        Assert.Empty(result.Session.LootedItems);
    }

    [Fact]
    public void Parse_LeavesMissingMetricsAndDatesNull()
    {
        var result = _parser.Parse("Session: 00:10h\nKilled Monsters: None\nLooted Items: None");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Session!.SessionStart);
        Assert.Null(result.Session.SessionEnd);
        Assert.Null(result.Session.Statistics.XpGain);
    }

    [Fact]
    public void Parse_ReportsInvalidNumbersAndDoesNotReturnPartialSession()
    {
        var result = _parser.Parse("XP Gain: many\nBalance: 1,2,3");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Contains(result.Errors, error => error.Field == "XP Gain");
        Assert.Contains(result.Errors, error => error.Field == "Balance");
    }

    [Fact]
    public void Parse_ReportsMalformedDatesAndListEntries()
    {
        const string text = """
            From someday to another day
            Killed Monsters:
              0x dragons
            """;

        var result = _parser.Parse(text);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Field == "Session dates");
        Assert.Contains(result.Errors, error => error.Field == "Killed Monsters line 1");
    }
}
