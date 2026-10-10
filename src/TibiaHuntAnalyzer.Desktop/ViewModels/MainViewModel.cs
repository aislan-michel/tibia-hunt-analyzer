using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Csv;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IHuntSessionService _huntSessionService;
    private readonly ICsvHuntSessionService _csvHuntSessionService;
    private readonly IHuntAnalyticsService _huntAnalyticsService;
    private readonly JsonHuntSessionParser _jsonParser;
    private readonly TextHuntSessionParser _textParser;

    public MainViewModel(
        ISettingsService settingsService,
        IHuntSessionService huntSessionService,
        ICsvHuntSessionService csvHuntSessionService,
        IHuntAnalyticsService huntAnalyticsService,
        JsonHuntSessionParser jsonParser,
        TextHuntSessionParser textParser)
    {
        _settingsService = settingsService;
        _huntSessionService = huntSessionService;
        _csvHuntSessionService = csvHuntSessionService;
        _huntAnalyticsService = huntAnalyticsService;
        _jsonParser = jsonParser;
        _textParser = textParser;
        CurrentViewModel = CreateHomeViewModel();
    }

    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    public bool IsHome => CurrentViewModel is HomeViewModel;

    private async Task OpenInsertAsync()
    {
        try
        {
            var viewModel = new InsertHuntSessionViewModel(
                _settingsService,
                _huntSessionService,
                _jsonParser,
                _textParser);
            viewModel.ExitRequested += (_, _) => ShutdownApplication();
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
            var settingsViewModel = new SettingsViewModel(_settingsService);
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

    private async Task OpenQueryAsync()
    {
        var viewModel = new SessionQueryViewModel(_huntSessionService, _csvHuntSessionService);
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
        return viewModel;
    }

    private static void ShutdownApplication()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
