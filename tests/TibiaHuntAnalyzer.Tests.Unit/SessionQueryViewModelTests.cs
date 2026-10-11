using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class SessionQueryViewModelTests
{
    [Fact]
    public async Task LoadAsync_OrdersNewestFirstAndFiltersByCharacterLocationAndDates()
    {
        var sessions = new[]
        {
            CreateSummary("Knight", "Banuta", new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 10, 12, 0, 0)))),
            CreateSummary("Druid", "Cave", new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 9, 12, 0, 0)))),
            CreateSummary("Elite Knight", "Banuta", new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 8, 12, 0, 0))))
        };
        var viewModel = new SessionQueryViewModel(new FakeHuntSessionService(sessions));

        await viewModel.LoadAsync();

        Assert.Equal(sessions[0].Id, viewModel.Sessions[0].Id);
        Assert.Equal(3, viewModel.Sessions.Count);

        viewModel.CharacterFilter = "knight";
        viewModel.HuntLocationFilter = "banuta";
        viewModel.StartDateFilter = "2026-10-08";
        viewModel.EndDateFilter = "2026-10-10";

        Assert.Equal(2, viewModel.Sessions.Count);
        Assert.Contains(viewModel.Sessions, session => session.CharacterName == "Knight");
        Assert.Contains(viewModel.Sessions, session => session.CharacterName == "Elite Knight");

        viewModel.SortByCommand.Execute(SessionSortField.Character);

        Assert.Equal("Elite Knight", viewModel.Sessions[0].CharacterName);
        Assert.Equal("Knight", viewModel.Sessions[1].CharacterName);
    }

    [Fact]
    public async Task LoadAsync_ReportsInvalidOrReversedDateFilters()
    {
        var viewModel = new SessionQueryViewModel(
            new FakeHuntSessionService([CreateSummary("Knight", "Banuta", DateTimeOffset.UtcNow)]));
        await viewModel.LoadAsync();

        viewModel.StartDateFilter = "10/10/2026";
        Assert.Empty(viewModel.Sessions);
        Assert.Contains("YYYY-MM-DD", viewModel.StatusMessage);

        viewModel.StartDateFilter = "2026-10-11";
        viewModel.EndDateFilter = "2026-10-10";
        Assert.Empty(viewModel.Sessions);
        Assert.Contains("must not be later", viewModel.StatusMessage);
    }

    [Fact]
    public async Task OpenDetailsCommand_LoadsAllMetricsAndRecordQuantities()
    {
        var summary = CreateSummary("Knight", "Banuta", DateTimeOffset.UtcNow);
        var details = new HuntSessionDetails(
            summary.Id,
            summary.CharacterName,
            Guid.NewGuid(),
            100,
            summary.HuntLocationName,
            Guid.NewGuid(),
            summary.SessionStart,
            summary.SessionEnd,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            SessionInputFormat.Text,
            "source input",
            new HuntStatistics(balance: -10m, xpGain: 100, rawXpGain: 150),
            [new SessionRecord("Dragon", 2)],
            [new SessionRecord("Gold coin", null)]);
        var viewModel = new SessionQueryViewModel(
            new FakeHuntSessionService([summary], details));
        await viewModel.LoadAsync();
        viewModel.SelectedSession = summary;

        await viewModel.OpenDetailsCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDetailsVisible);
        Assert.Equal(-10m, viewModel.SelectedDetails!.Statistics.Balance);
        Assert.Equal(100, viewModel.SelectedDetails.Statistics.XpGain);
        Assert.Equal(150, viewModel.SelectedDetails.Statistics.RawXpGain);
        Assert.Equal("2 x Dragon", viewModel.DetailMonsters.Single().Display);
        Assert.Equal("Gold coin", viewModel.DetailItems.Single().Display);

        viewModel.BackToSessionsCommand.Execute(null);
        Assert.False(viewModel.IsDetailsVisible);
        Assert.Null(viewModel.SelectedDetails);
    }

    private static HuntSessionSummary CreateSummary(
        string character,
        string location,
        DateTimeOffset start)
    {
        return new HuntSessionSummary(
            Guid.NewGuid(),
            character,
            100,
            location,
            start,
            start.AddMinutes(30));
    }

    private sealed class FakeHuntSessionService(
        IReadOnlyList<HuntSessionSummary> summaries,
        HuntSessionDetails? details = null) : IHuntSessionService
    {
        public Task<Guid> SaveAsync(
            SaveHuntSessionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task<HuntSessionDetails?> GetDetailsAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(details?.Id == sessionId ? details : null);

        public Task<IReadOnlyList<HuntSessionSummary>> GetSummariesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(summaries);
    }

}
