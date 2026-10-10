namespace TibiaHuntAnalyzer.Application.Settings;

public sealed record SaveSettingsRequest(
    IReadOnlyList<SettingItem> Characters,
    IReadOnlyList<SettingItem> HuntLocations);
