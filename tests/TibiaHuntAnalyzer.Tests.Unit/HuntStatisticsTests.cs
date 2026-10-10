using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class HuntStatisticsTests
{
    [Fact]
    public void Constructor_PreservesReportedMetricsAndMissingValues()
    {
        var statistics = new HuntStatistics(
            balance: -12.5m,
            damage: 1000,
            damagePerHour: 2000.5m,
            healing: 700,
            healingPerHour: 800.25m,
            loot: 300,
            supplies: 312.5m,
            xpGain: 4000,
            xpPerHour: 8000.5m,
            rawXpGain: 5000,
            rawXpPerHour: 10000.75m);

        Assert.Equal(-12.5m, statistics.Balance);
        Assert.Equal(1000, statistics.Damage);
        Assert.Equal(2000.5m, statistics.DamagePerHour);
        Assert.Equal(700, statistics.Healing);
        Assert.Equal(800.25m, statistics.HealingPerHour);
        Assert.Equal(300, statistics.Loot);
        Assert.Equal(312.5m, statistics.Supplies);
        Assert.Equal(4000, statistics.XpGain);
        Assert.Equal(8000.5m, statistics.XpPerHour);
        Assert.Equal(5000, statistics.RawXpGain);
        Assert.Equal(10000.75m, statistics.RawXpPerHour);
    }

    [Fact]
    public void Constructor_LeavesMissingMetricsNull()
    {
        var statistics = new HuntStatistics();

        Assert.Null(statistics.Balance);
        Assert.Null(statistics.XpGain);
        Assert.Null(statistics.RawXpGain);
    }

    [Theory]
    [InlineData(-1L)]
    public void Constructor_RejectsNegativeCounts(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HuntStatistics(xpGain: value));
    }

    [Fact]
    public void Constructor_AllowsZeroStatistics()
    {
        var statistics = new HuntStatistics(xpGain: 0, damage: 0);

        Assert.Equal(0, statistics.XpGain);
        Assert.Equal(0, statistics.Damage);
    }

    [Fact]
    public void Constructor_AllowsNegativeBalance()
    {
        var statistics = new HuntStatistics(balance: -1m);

        Assert.Equal(-1m, statistics.Balance);
    }
}
