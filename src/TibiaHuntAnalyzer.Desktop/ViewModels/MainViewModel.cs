using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Backups;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Desktop;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IHuntSessionService _huntSessionService;
    private readonly IHuntAnalyticsService _huntAnalyticsService;
    private readonly IDatabaseBackupService _databaseBackupService;
    private readonly IDatabasePathSettingsService _databasePathSettingsService;
    private readonly JsonHuntSessionParser _jsonParser;
    private readonly TextHuntSessionParser _textParser;

    public MainViewModel(
        ISettingsService settingsService,
        IHuntSessionService huntSessionService,
        IHuntAnalyticsService huntAnalyticsService,
        IDatabaseBackupService databaseBackupService,
        IDatabasePathSettingsService databasePathSettingsService,
        JsonHuntSessionParser jsonParser,
        TextHuntSessionParser textParser,
        DatabaseStartupStatus startupStatus)
    {
        _settingsService = settingsService;
        _huntSessionService = huntSessionService;
        _huntAnalyticsService = huntAnalyticsService;
        _databaseBackupService = databaseBackupService;
        _databasePathSettingsService = databasePathSettingsService;
        _jsonParser = jsonParser;
        _textParser = textParser;
        CurrentViewModel = startupStatus.HasError
            ? CreateDatabaseRecoveryViewModel(startupStatus.ErrorMessage!)
            : CreateHomeViewModel();
    }

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public bool IsHome => CurrentViewModel is HomeViewModel;

    public event EventHandler? ApplicationExitRequested;

    private async Task OpenInsertAsync()
    {
        var previousViewModel = CurrentViewModel;
        try
        {
            var viewModel = new InsertHuntSessionViewModel(
                _settingsService,
                _huntSessionService,
                _jsonParser,
                _textParser);
            viewModel.ExitRequested += (_, _) => CurrentViewModel = previousViewModel;
            await viewModel.LoadAsync();
            CurrentViewModel = viewModel;
        }
        catch (Exception exception)
        {
            CurrentViewModel = new MessageViewModel(
                $"Could not load active characters and hunt locations: {exception.Message}",
                () => CurrentViewModel = CreateHomeViewModel());
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            var settingsViewModel = new SettingsViewModel(_databasePathSettingsService);
            settingsViewModel.ExitRequested += (_, _) => CurrentViewModel = CreateHomeViewModel();
            await settingsViewModel.LoadAsync();
            CurrentViewModel = settingsViewModel;
        }
        catch (Exception exception)
        {
            CurrentViewModel = new MessageViewModel(
                $"Could not load settings: {exception.Message}",
                () => CurrentViewModel = CreateHomeViewModel());
        }
    }

    private async Task OpenRecoverySettingsAsync(SettingsViewModel settingsViewModel)
    {
        settingsViewModel.ExitRequested += (_, _) => ApplicationExitRequested?.Invoke(this, EventArgs.Empty);
        await settingsViewModel.LoadAsync();
        CurrentViewModel = settingsViewModel;
    }

    private DatabaseRecoveryViewModel CreateDatabaseRecoveryViewModel(string errorMessage)
    {
        var viewModel = new DatabaseRecoveryViewModel(errorMessage);
        viewModel.ConfigureDatabaseRequested += async (_, _) =>
        {
            var settingsViewModel = new SettingsViewModel(
                _databasePathSettingsService,
                startupError: errorMessage);
            await OpenRecoverySettingsAsync(settingsViewModel);
        };
        viewModel.ExitRequested += (_, _) => ApplicationExitRequested?.Invoke(this, EventArgs.Empty);
        return viewModel;
    }

    private async Task OpenQueryAsync()
    {
        var viewModel = new SessionQueryViewModel(_huntSessionService);
        viewModel.ExitRequested += (_, _) => CurrentViewModel = CreateHomeViewModel();
        await viewModel.LoadAsync();
        CurrentViewModel = viewModel;
    }

    private async Task OpenAnalyticsAsync()
    {
        var viewModel = new AnalyticsViewModel(_huntAnalyticsService, _settingsService);
        viewModel.ExitRequested += (_, _) => CurrentViewModel = CreateHomeViewModel();
        await viewModel.LoadAsync();
        CurrentViewModel = viewModel;
    }

    private void OpenBackupRestore()
    {
        var viewModel = new BackupRestoreViewModel(_databaseBackupService);
        viewModel.ExitRequested += (_, _) => CurrentViewModel = CreateHomeViewModel();
        CurrentViewModel = viewModel;
    }

    private async Task OpenSettingManagementAsync(ManagedSettingKind kind)
    {
        var viewModel = new SettingManagementViewModel(_settingsService, kind);
        viewModel.ExitRequested += (_, _) => CurrentViewModel = CreateHomeViewModel();
        try
        {
            await viewModel.LoadAsync();
            CurrentViewModel = viewModel;
        }
        catch (Exception exception)
        {
            CurrentViewModel = new MessageViewModel(
                $"Could not load {viewModel.Title.ToLowerInvariant()}: {exception.Message}",
                () => CurrentViewModel = CreateHomeViewModel());
        }
    }

    partial void OnCurrentViewModelChanged(ViewModelBase value)
    {
        OnPropertyChanged(nameof(IsHome));
    }

    private HomeViewModel CreateHomeViewModel()
    {
        var viewModel = new HomeViewModel();
        viewModel.InsertRequested += async (_, _) => await OpenInsertAsync();
        viewModel.SettingsRequested += async (_, _) => await OpenSettingsAsync();
        viewModel.QueryRequested += async (_, _) => await OpenQueryAsync();
        viewModel.AnalyticsRequested += async (_, _) => await OpenAnalyticsAsync();
        viewModel.BackupRestoreRequested += (_, _) => OpenBackupRestore();
        viewModel.ManageCharactersRequested += async (_, _) =>
            await OpenSettingManagementAsync(ManagedSettingKind.Character);
        viewModel.ManageHuntLocationsRequested += async (_, _) =>
            await OpenSettingManagementAsync(ManagedSettingKind.HuntLocation);
        viewModel.ExitRequested += (_, _) => ApplicationExitRequested?.Invoke(this, EventArgs.Empty);
        return viewModel;
    }

}
