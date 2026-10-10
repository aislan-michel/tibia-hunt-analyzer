namespace TibiaHuntAnalyzer.Application.Settings;

public interface ISettingsService
{
    Task<IReadOnlyList<SettingItem>> GetCharactersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettingItem>> GetHuntLocationsAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default);
}
