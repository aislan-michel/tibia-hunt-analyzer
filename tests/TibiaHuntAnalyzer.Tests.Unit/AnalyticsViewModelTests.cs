using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class AnalyticsViewModelTests
{
    [Fact]
    public async Task LoadAsync_LoadsActiveFilterOptionsAndInitialReport()
    {
        var characterId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var analytics = new FakeAnalyticsService();
        var settings = new FakeSettingsService(
            [new SettingItem(characterId, "Knight", true), new SettingItem(Guid.NewGuid(), "Inactive", false)],
            [new SettingItem(locationId, "Banuta", true)]);
        var viewModel = new AnalyticsViewModel(analytics, settings);

        await viewModel.LoadAsync();

        Assert.Collection(
            viewModel.Characters,
            option => Assert.Equal("All characters", option.Name),
            option => Assert.Equal("Knight", option.Name));
        Assert.Collection(
            viewModel.HuntLocations,
            option => Assert.Equal("All hunt locations", option.Name),
            option => Assert.Equal("Banuta", option.Name));
        Assert.Null(Assert.Single(analytics.Requests).CharacterId);
    }

    [Fact]
    public async Task AnalyzeCommand_SendsSelectedFiltersAndRejectsInvalidDateRange()
    {
        var characterId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var analytics = new FakeAnalyticsService();
        var viewModel = new AnalyticsViewModel(
            analytics,
            new FakeSettingsService(
                [new SettingItem(characterId, "Knight", true)],
                [new SettingItem(locationId, "Banuta", true)]));
        await viewModel.LoadAsync();
        analytics.Requests.Clear();

        viewModel.SelectedCharacter = viewModel.Characters[1];
        viewModel.SelectedHuntLocation = viewModel.HuntLocations[1];
        viewModel.StartDateFilter = "2026-10-10";
        viewModel.EndDateFilter = "2026-10-12";
        await viewModel.AnalyzeCommand.ExecuteAsync(null);

        var appliedFilter = Assert.Single(analytics.Requests);
        Assert.Equal(characterId, appliedFilter.CharacterId);
        Assert.Equal(locationId, appliedFilter.HuntLocationId);
        Assert.Equal(new DateOnly(2026, 10, 10), appliedFilter.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 12), appliedFilter.EndDate);

        viewModel.StartDateFilter = "2026-10-13";
        viewModel.EndDateFilter = "2026-10-12";
        await viewModel.AnalyzeCommand.ExecuteAsync(null);

        Assert.Single(analytics.Requests);
        Assert.Contains("must not be later", viewModel.StatusMessage);
    }

    [Fact]
    public async Task AnalyzeCommand_FormatsMissingMetricsWithoutInventingValues()
    {
        var viewModel = new AnalyticsViewModel(
            new FakeAnalyticsService(CreateReport()),
            new FakeSettingsService([], []));

        await viewModel.LoadAsync();

        Assert.Equal(1, viewModel.SessionCount);
        Assert.Equal("— (0 session(s))", viewModel.BalanceTotal);
        Assert.Equal("— (0 session(s))", viewModel.XpTotal);
        Assert.Contains("1 session", viewModel.StatusMessage);
    }

    private static HuntAnalyticsReport CreateReport()
    {
        var metrics = new AnalyticsAggregate(
            1,
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null),
            new AnalyticsMetric(0, null, null));
        return new HuntAnalyticsReport([], metrics, [], [], []);
    }

    private sealed class FakeAnalyticsService(HuntAnalyticsReport? report = null) : IHuntAnalyticsService
    {
        public List<HuntAnalyticsFilter> Requests { get; } = [];

        public Task<HuntAnalyticsReport> GetReportAsync(
            HuntAnalyticsFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(filter ?? new HuntAnalyticsFilter());
            return Task.FromResult(report ?? CreateReport());
        }
    }

    private sealed class FakeSettingsService(
        IReadOnlyList<SettingItem> characters,
        IReadOnlyList<SettingItem> huntLocations) : ISettingsService
    {
        public Task<IReadOnlyList<SettingItem>> GetCharactersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(characters);

        public Task<IReadOnlyList<SettingItem>> GetHuntLocationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(huntLocations);

        public Task SaveAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
