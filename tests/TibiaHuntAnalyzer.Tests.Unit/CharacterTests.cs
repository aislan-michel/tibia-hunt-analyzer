using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class CharacterTests
{
    [Fact]
    public void Constructor_TrimsNameAndCreatesActiveCharacter()
    {
        var character = new Character("  Knight  ");

        Assert.NotEqual(Guid.Empty, character.Id);
        Assert.Equal("Knight", character.Name);
        Assert.True(character.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<ArgumentException>(() => new Character(name));
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var character = new Character("Knight");

        character.Rename("  Elite Knight ");

        Assert.Equal("Elite Knight", character.Name);
    }

    [Fact]
    public void DeactivateAndReactivate_PreserveCharacterIdentity()
    {
        var character = new Character("Knight");
        var id = character.Id;

        character.Deactivate();
        Assert.False(character.IsActive);

        character.Reactivate();

        Assert.True(character.IsActive);
        Assert.Equal(id, character.Id);
    }
}
