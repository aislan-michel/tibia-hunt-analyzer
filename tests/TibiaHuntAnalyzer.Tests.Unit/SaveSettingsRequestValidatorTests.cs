using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class SaveSettingsRequestValidatorTests
{
    private readonly SaveSettingsRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsBlankCharacterAndLocationNames()
    {
        var request = new SaveSettingsRequest(
            [new SettingItem(null, " ", true)],
            [new SettingItem(null, "", true)]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_RejectsNamesLongerThanTwoHundredCharacters()
    {
        var request = new SaveSettingsRequest(
            [new SettingItem(null, new string('a', 201), true)],
            []);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("200"));
    }

    [Fact]
    public void Validate_AllowsDuplicateNames()
    {
        var request = new SaveSettingsRequest(
            [
                new SettingItem(null, "Knight", true),
                new SettingItem(null, "Knight", true)
            ],
            []);

        Assert.True(_validator.Validate(request).IsValid);
    }
}
