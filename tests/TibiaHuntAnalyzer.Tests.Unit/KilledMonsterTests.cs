using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class KilledMonsterTests
{
    [Fact]
    public void Constructor_AllowsOmittedCountWithoutInventingIt()
    {
        var monster = new KilledMonster("Dragon", null);

        Assert.Null(monster.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new KilledMonster(name, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveCount(long count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KilledMonster("Dragon", count));
    }
}
