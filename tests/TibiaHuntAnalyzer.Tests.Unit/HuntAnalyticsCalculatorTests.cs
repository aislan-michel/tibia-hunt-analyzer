using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class HuntAnalyticsCalculatorTests
{
    [Fact]
    public void Calculate_SeparatesImportedMetricsFromRatesCalculatedFromReportedDuration()
    {
        var characterId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var session = CreateSession(
            "Knight",
            characterId,
            "Banuta",
            locationId,
            new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero),
            TimeSpan.FromMinutes(30),
            new HuntStatistics(
                balance: -60m,
                xpGain: 1_000,
                xpPerHour: 2_500m,
                rawXpGain: 1_200,
                rawXpPerHour: 3_000m));

        var report = HuntAnalyticsCalculator.Calculate([session]);

        var analysis = Assert.Single(report.Sessions);
        Assert.Equal(-60m, analysis.Balance);
        Assert.Equal(-120m, analysis.CalculatedBalancePerHour);
        Assert.Equal(2_500m, analysis.ReportedXpPerHour);
        Assert.Equal(2_000m, analysis.CalculatedXpPerHour);
        Assert.Equal(3_000m, analysis.ReportedRawXpPerHour);
        Assert.Equal(2_400m, analysis.CalculatedRawXpPerHour);
        Assert.Equal(1, report.Overall.XpGain.ContributingSessionCount);
        Assert.Equal(1_000m, report.Overall.XpGain.Total);
        Assert.Equal(2_000m, report.Overall.CalculatedXpPerHour.Average);
    }

    [Fact]
    public void Calculate_ExcludesMissingMetricsAndZeroDurationFromRateAverages()
    {
        var characterId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var first = CreateSession(
            "Knight",
            characterId,
            "Banuta",
            locationId,
            new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero),
            TimeSpan.FromHours(1),
            new HuntStatistics(balance: 100m, xpGain: 10));
        var zeroDuration = CreateSession(
            "Knight",
            characterId,
            "Banuta",
            locationId,
            new DateTimeOffset(2026, 10, 11, 10, 0, 0, TimeSpan.Zero),
            TimeSpan.Zero,
            new HuntStatistics(balance: 50m, xpGain: 20, xpPerHour: 500m));

        var report = HuntAnalyticsCalculator.Calculate([first, zeroDuration]);

        Assert.Equal(2, report.Overall.SessionCount);
        Assert.Equal(2, report.Overall.Balance.ContributingSessionCount);
        Assert.Equal(150m, report.Overall.Balance.Total);
        Assert.Equal(1, report.Overall.CalculatedXpPerHour.ContributingSessionCount);
        Assert.Equal(10m, report.Overall.CalculatedXpPerHour.Average);
        Assert.Equal(1, report.Overall.ReportedXpPerHour.ContributingSessionCount);
        Assert.Null(report.Sessions.Single(session => session.SessionStart.Day == 11).CalculatedXpPerHour);
        Assert.Equal(500m, report.Sessions.Single(session => session.SessionStart.Day == 11).ReportedXpPerHour);
    }

    [Fact]
    public void Calculate_FiltersAndGroupsByCharacterLocationAndLocalStartDate()
    {
        var knightId = Guid.NewGuid();
        var druidId = Guid.NewGuid();
        var banutaId = Guid.NewGuid();
        var caveId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);
        var sessions = new[]
        {
            CreateSession("Knight", knightId, "Banuta", banutaId, start, TimeSpan.FromHours(1),
                new HuntStatistics(balance: 100m, xpGain: 1_000)),
            CreateSession("Knight", knightId, "Banuta", banutaId, start.AddDays(1), TimeSpan.FromHours(1),
                new HuntStatistics(balance: 200m, xpGain: 2_000)),
            CreateSession("Druid", druidId, "Cave", caveId, start, TimeSpan.FromHours(1),
                new HuntStatistics(balance: 300m, xpGain: 3_000))
        };

        var report = HuntAnalyticsCalculator.Calculate(
            sessions,
            new HuntAnalyticsFilter(
                CharacterId: knightId,
                HuntLocationId: banutaId,
                StartDate: new DateOnly(2026, 10, 10),
                EndDate: new DateOnly(2026, 10, 10)));

        Assert.Single(report.Sessions);
        Assert.Equal(100m, report.Overall.Balance.Total);
        Assert.Equal(1, Assert.Single(report.ByCharacter).Metrics.SessionCount);
        Assert.Equal("Banuta", Assert.Single(report.ByHuntLocation).Name);
        Assert.Equal(100, Assert.Single(report.LevelProgression).CharacterLevel);
    }

    [Fact]
    public void Calculate_RejectsReversedDateRange()
    {
        Assert.Throws<ArgumentException>(() => HuntAnalyticsCalculator.Calculate(
            [],
            new HuntAnalyticsFilter(
                StartDate: new DateOnly(2026, 10, 11),
                EndDate: new DateOnly(2026, 10, 10))));
    }

    private static HuntSessionDetails CreateSession(
        string characterName,
        Guid characterId,
        string locationName,
        Guid locationId,
        DateTimeOffset start,
        TimeSpan duration,
        HuntStatistics statistics)
    {
        var id = Guid.NewGuid();
        return new HuntSessionDetails(
            id,
            characterName,
            characterId,
            100,
            locationName,
            locationId,
            start,
            start.Add(duration),
            duration,
            duration,
            SessionInputFormat.Json,
            string.Empty,
            statistics,
            [],
            []);
    }
}
