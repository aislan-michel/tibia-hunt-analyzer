using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class HuntLocationTests
{
    [Fact]
    public void Constructor_TrimsNameAndCreatesActiveLocation()
    {
        var location = new HuntLocation("  Banuta  ");

        Assert.NotEqual(Guid.Empty, location.Id);
        Assert.Equal("Banuta", location.Name);
        Assert.True(location.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new HuntLocation(name));
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var location = new HuntLocation("Banuta");

        location.Rename("  Deeper Banuta ");

        Assert.Equal("Deeper Banuta", location.Name);
    }

    [Fact]
    public void DeactivateAndReactivate_PreserveLocationIdentity()
    {
        var location = new HuntLocation("Banuta");
        var id = location.Id;

        location.Deactivate();
        Assert.False(location.IsActive);

        location.Reactivate();

        Assert.True(location.IsActive);
        Assert.Equal(id, location.Id);
    }
}
