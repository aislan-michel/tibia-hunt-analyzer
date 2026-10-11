using System.ComponentModel;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Backups;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Desktop;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Tests.Unit;

public class MainViewModelNavigationTests
{
    [Fact]
    public async Task HomeCommands_OpenDedicatedCharacterAndLocationManagementScreens()
    {
        var viewModel = CreateMainViewModel();
        var home = Assert.IsType<HomeViewModel>(viewModel.CurrentViewModel);

        var characters = await NavigateTo<SettingManagementViewModel>(
            viewModel,
            () => home.ManageCharactersCommand.Execute(null));
        Assert.Equal("Gerenciar Personagens", characters.Title);

        home = await NavigateTo<HomeViewModel>(viewModel, () => characters.ExitCommand.Execute(null));
        var locations = await NavigateTo<SettingManagementViewModel>(
            viewModel,
            () => home.ManageHuntLocationsCommand.Execute(null));
        Assert.Equal("Gerenciar Locais de Hunt", locations.Title);
    }

    [Fact]
    public async Task InsertExit_ReturnsToPreviousHomeWithoutRequestingApplicationShutdown()
    {
        var viewModel = CreateMainViewModel();
        var home = Assert.IsType<HomeViewModel>(viewModel.CurrentViewModel);
        var shutdownRequested = false;
        viewModel.ApplicationExitRequested += (_, _) => shutdownRequested = true;

        var insert = await NavigateTo<InsertHuntSessionViewModel>(
            viewModel,
            () => home.InsertCommand.Execute(null));
        insert.ExitCommand.Execute(null);

        Assert.Same(home, viewModel.CurrentViewModel);
        Assert.False(shutdownRequested);
    }

    [Fact]
    public async Task HomeGearAndQueryCommands_ContinueOpeningSettingsAndSessionQuery()
    {
        var viewModel = CreateMainViewModel();
        var home = Assert.IsType<HomeViewModel>(viewModel.CurrentViewModel);

        var settings = await NavigateTo<SettingsViewModel>(
            viewModel,
            () => viewModel.OpenSettingsCommand.Execute(null));
        Assert.Equal("C:\\data\\active.db", settings.ActiveDatabasePath);

        home = await NavigateTo<HomeViewModel>(viewModel, () => settings.ExitCommand.Execute(null));
        var query = await NavigateTo<SessionQueryViewModel>(
            viewModel,
            () => home.QueryCommand.Execute(null));
        Assert.IsType<SessionQueryViewModel>(query);
    }

    private static async Task<T> NavigateTo<T>(MainViewModel viewModel, Action navigate)
        where T : ViewModelBase
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(MainViewModel.CurrentViewModel) &&
                viewModel.CurrentViewModel is T destination)
            {
                completion.TrySetResult(destination);
            }
        }

        viewModel.PropertyChanged += OnPropertyChanged;
        try
        {
            navigate();
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            viewModel.PropertyChanged -= OnPropertyChanged;
        }
    }

    private static MainViewModel CreateMainViewModel() =>
        new(
            new FakeSettingsService(),
            new FakeHuntSessionService(),
            new FakeAnalyticsService(),
            new FakeBackupService(),
            new FakeDatabasePathSettingsService(),
            new JsonHuntSessionParser(),
            new TextHuntSessionParser(),
            new DatabaseStartupStatus());

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<IReadOnlyList<SettingItem>> GetCharactersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SettingItem>>([]);

        public Task<IReadOnlyList<SettingItem>> GetHuntLocationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SettingItem>>([]);

        public Task SaveAsync(SaveSettingsRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeHuntSessionService : IHuntSessionService
    {
        public Task<Guid> SaveAsync(
            SaveHuntSessionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task<HuntSessionDetails?> GetDetailsAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<HuntSessionDetails?>(null);

        public Task<IReadOnlyList<HuntSessionSummary>> GetSummariesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HuntSessionSummary>>([]);
    }

    private sealed class FakeAnalyticsService : IHuntAnalyticsService
    {
        public Task<HuntAnalyticsReport> GetReportAsync(
            HuntAnalyticsFilter? filter = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new HuntAnalyticsReport(
                [],
                new AnalyticsAggregate(
                    0,
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric(),
                    EmptyMetric()),
                [],
                [],
                []));

        private static AnalyticsMetric EmptyMetric() => new(0, null, null);
    }

    private sealed class FakeBackupService : IDatabaseBackupService
    {
        public Task CreateBackupAsync(Stream destination, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> RestoreBackupAsync(Stream source, CancellationToken cancellationToken = default) =>
            Task.FromResult("backup.db");
    }

    private sealed class FakeDatabasePathSettingsService : IDatabasePathSettingsService
    {
        public string ActiveDatabasePath => "C:\\data\\active.db";

        public Task<string> GetConfiguredDatabasePathAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ActiveDatabasePath);

        public Task ConfigureDatabasePathAsync(
            string databasePath,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
