using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class LootedItemTests
{
    [Fact]
    public void Constructor_AllowsOmittedQuantityWithoutInventingIt()
    {
        var item = new LootedItem("Dragon Ham", null);

        Assert.Null(item.Quantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new LootedItem(name, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveQuantity(long quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LootedItem("Dragon Ham", quantity));
    }
}
