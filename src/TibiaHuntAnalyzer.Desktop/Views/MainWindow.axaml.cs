using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TibiaHuntAnalyzer.Desktop.ViewModels;

namespace TibiaHuntAnalyzer.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        _viewModel = null!;
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        _viewModel.ApplicationExitRequested += (_, _) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };
        AttachToInsertViewModel();
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
        {
            AttachToInsertViewModel();
        }
    }

    private void AttachToInsertViewModel()
    {
        if (_viewModel.CurrentViewModel is InsertHuntSessionViewModel insertViewModel)
        {
            insertViewModel.FileSelectionRequested += async (_, _) =>
                await SelectJsonFileAsync(insertViewModel);
        }

        if (_viewModel.CurrentViewModel is BackupRestoreViewModel backupRestoreViewModel)
        {
            backupRestoreViewModel.BackupRequested += async (_, _) =>
                await SaveDatabaseBackupAsync(backupRestoreViewModel);
            backupRestoreViewModel.RestoreRequested += async (_, _) =>
                await SelectDatabaseBackupAsync(backupRestoreViewModel);
        }

        if (_viewModel.CurrentViewModel is SettingsViewModel settingsViewModel)
        {
            settingsViewModel.ExistingDatabaseBrowseRequested += async (_, _) =>
                await SelectExistingDatabaseAsync(settingsViewModel);
            settingsViewModel.NewDatabaseBrowseRequested += async (_, _) =>
                await SelectNewDatabaseLocationAsync(settingsViewModel);
        }
    }

    private async Task SelectJsonFileAsync(InsertHuntSessionViewModel viewModel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a Tibia Hunt Analyzer JSON file",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON files")
                    {
                        Patterns = ["*.json"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            await viewModel.LoadJsonFileAsync(files[0].Name, stream);
        }
        catch (Exception exception)
        {
            viewModel.ReportFileError(exception.Message);
        }
    }

    private async Task SaveDatabaseBackupAsync(BackupRestoreViewModel viewModel)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Create Tibia Hunt Analyzer database backup",
                SuggestedFileName = $"tibia-hunt-analyzer-backup-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.db",
                DefaultExtension = "db",
                FileTypeChoices =
                [
                    new FilePickerFileType("SQLite database backups")
                    {
                        Patterns = ["*.db", "*.sqlite"]
                    }
                ]
            });

            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenWriteAsync();
            await viewModel.CreateBackupAsync(stream);
        }
        catch (Exception exception)
        {
            viewModel.ReportFileError(exception.Message);
        }
    }

    private async Task SelectDatabaseBackupAsync(BackupRestoreViewModel viewModel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select a Tibia Hunt Analyzer database backup",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("SQLite database backups")
                    {
                        Patterns = ["*.db", "*.sqlite"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using var stream = await files[0].OpenReadAsync();
            await viewModel.RestoreBackupAsync(stream);
        }
        catch (Exception exception)
        {
            viewModel.ReportFileError(exception.Message);
        }
    }

    private async Task SelectExistingDatabaseAsync(SettingsViewModel viewModel)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select an existing Tibia Hunt Analyzer database",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("SQLite databases")
                    {
                        Patterns = ["*.db", "*.sqlite"]
                    }
                ]
            });

            if (files.Count > 0)
            {
                viewModel.SelectDatabasePath(files[0].Path.LocalPath);
            }
        }
        catch (Exception exception)
        {
            viewModel.ReportBrowseError(exception.Message);
        }
    }

    private async Task SelectNewDatabaseLocationAsync(SettingsViewModel viewModel)
    {
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Choose a location for a new Tibia Hunt Analyzer database",
                SuggestedFileName = "tibia-hunt-analyzer.db",
                DefaultExtension = "db",
                FileTypeChoices =
                [
                    new FilePickerFileType("SQLite databases")
                    {
                        Patterns = ["*.db", "*.sqlite"]
                    }
                ]
            });

            if (file is not null)
            {
                viewModel.SelectDatabasePath(file.Path.LocalPath);
            }
        }
        catch (Exception exception)
        {
            viewModel.ReportBrowseError(exception.Message);
        }
    }
}