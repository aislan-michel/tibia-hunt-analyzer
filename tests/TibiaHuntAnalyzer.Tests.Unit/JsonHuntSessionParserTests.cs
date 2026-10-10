using TibiaHuntAnalyzer.Application.Imports;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class JsonHuntSessionParserTests
{
    private readonly JsonHuntSessionParser _parser = new();

    [Fact]
    public void Parse_ConvertsSupportedFieldsAndPreservesOriginalInput()
    {
        const string json = """
            {
              "Balance": "-125.5",
              "Damage": "12000",
              "Damage/h": 24000,
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
              "Supplies": 625.5,
              "XP Gain": "8000",
              "XP/h": 16000
            }
            """;

        var result = _parser.Parse(json);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Session);
        Assert.Equal(HuntInputFormat.Json, result.Session.SourceFormat);
        Assert.Equal(json, result.Session.OriginalContent);
        Assert.Equal(DateTimeOffset.Parse("2026-10-10T10:00:00Z"), result.Session.SessionStart);
        Assert.Equal(DateTimeOffset.Parse("2026-10-10T11:00:00Z"), result.Session.SessionEnd);
        Assert.Equal(TimeSpan.FromMinutes(45), result.Session.Duration);
        Assert.Equal(-125.5m, result.Session.Statistics.Balance);
        Assert.Equal(12000, result.Session.Statistics.Damage);
        Assert.Equal(24000, result.Session.Statistics.DamagePerHour);
        Assert.Equal(3500, result.Session.Statistics.Healing);
        Assert.Equal(7000, result.Session.Statistics.HealingPerHour);
        Assert.Equal(500, result.Session.Statistics.Loot);
        Assert.Equal(625.5m, result.Session.Statistics.Supplies);
        Assert.Equal(8000, result.Session.Statistics.XpGain);
        Assert.Equal(16000, result.Session.Statistics.XpPerHour);
        Assert.Equal(9000, result.Session.Statistics.RawXpGain);
        Assert.Equal(18000, result.Session.Statistics.RawXpPerHour);
        Assert.Collection(
            result.Session.KilledMonsters,
            monster =>
            {
                Assert.Equal("Dragon", monster.Name);
                Assert.Equal(2, monster.Count);
            });
        Assert.Collection(
            result.Session.LootedItems,
            item =>
            {
                Assert.Equal("Dragon Ham", item.Name);
                Assert.Equal(3, item.Quantity);
            });
    }

    [Theory]
    [InlineData("{")]
    [InlineData("not JSON")]
    public void Parse_ReturnsUsefulErrorForInvalidJson(string json)
    {
        var result = _parser.Parse(json);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Contains(result.Errors, error => error.Field == "$");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("\"session\"")]
    public void Parse_RejectsNonObjectRoot(string json)
    {
        var result = _parser.Parse(json);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Field == "$");
    }

    [Fact]
    public void Parse_MissingFieldsAndEmptyListsRemainMissingAndEmpty()
    {
        var result = _parser.Parse("""{"Killed Monsters":[],"Looted Items":[]}""");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Session);
        Assert.Null(result.Session.SessionStart);
        Assert.Null(result.Session.Duration);
        Assert.Null(result.Session.Statistics.XpGain);
        Assert.Empty(result.Session.KilledMonsters);
        Assert.Empty(result.Session.LootedItems);
    }

    [Fact]
    public void Parse_ReportsInvalidNumericFieldsWithoutProducingSession()
    {
        var result = _parser.Parse("""{"Balance":"unknown","XP Gain":"1.5","Loot":-1}""");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Session);
        Assert.Contains(result.Errors, error => error.Field == "Balance");
        Assert.Contains(result.Errors, error => error.Field == "XP Gain");
        Assert.Contains(result.Errors, error => error.Field == "Loot");
    }

    [Fact]
    public void Parse_ReportsInvalidDatesAndDurations()
    {
        var result = _parser.Parse(
            """{"Session start":42,"Session end":"tomorrow-ish","Session length":"sometime"}""");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Field == "Session start");
        Assert.Contains(result.Errors, error => error.Field == "Session end");
        Assert.Contains(result.Errors, error => error.Field == "Session length");
    }

    [Theory]
    [InlineData("1h 30min", 90)]
    [InlineData("01:30h", 90)]
    public void Parse_SupportsTibiaDurationRepresentations(string duration, int expectedMinutes)
    {
        var result = _parser.Parse($$"""{"Session length":"{{duration}}"}""");

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), result.Session!.Duration);
    }

    [Fact]
    public void Parse_ReportsUnexpectedListTypesAndMalformedEntries()
    {
        var result = _parser.Parse(
            """{"Killed Monsters":"None","Looted Items":[{"Name":"Dragon Ham","Amount":"many"}]}""");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Field == "Killed Monsters");
        Assert.Contains(result.Errors, error => error.Field == "Looted Items[0]");
    }

    [Fact]
    public void Parse_DoesNotInventMissingMonsterCountsOrItemQuantities()
    {
        var result = _parser.Parse(
            """{"Killed Monsters":[{"Name":"Dragon"}],"Looted Items":[{"Name":"Dragon Ham"}]}""");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Session!.KilledMonsters[0].Count);
        Assert.Null(result.Session.LootedItems[0].Quantity);
    }
}
