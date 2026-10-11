using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class SettingsViewModel(
    IDatabasePathSettingsService databasePathSettingsService,
    string? startupError = null) : ViewModelBase
{
    private string? _savedPath;
    private bool _exitConfirmationPending;

    public string ActiveDatabasePath => databasePathSettingsService.ActiveDatabasePath;

    public bool IsRecoveryMode => !string.IsNullOrWhiteSpace(startupError);

    public string ExitButtonText => IsRecoveryMode ? "Fechar aplicativo" : "Sair";

    [ObservableProperty]
    public partial string DatabasePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    [ObservableProperty]
    public partial bool RestartRequired { get; private set; }

    public event EventHandler? ExistingDatabaseBrowseRequested;

    public event EventHandler? NewDatabaseBrowseRequested;

    public event EventHandler? ExitRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            DatabasePath = await databasePathSettingsService.GetConfiguredDatabasePathAsync(cancellationToken);
            _savedPath = DatabasePath;
            StatusMessage = startupError ?? string.Empty;
        }
        catch (Exception exception)
        {
            DatabasePath = ActiveDatabasePath;
            _savedPath = null;
            StatusMessage =
                $"Could not read the saved database path: {exception.Message} Select a valid database path to repair the configuration.";
        }

        HasUnsavedChanges = false;
        RestartRequired = false;
        _exitConfirmationPending = false;
    }

    [RelayCommand]
    private void BrowseExistingDatabase() =>
        ExistingDatabaseBrowseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void BrowseNewDatabaseLocation() =>
        NewDatabaseBrowseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            StatusMessage = "Select a database file or a location for a new database.";
            return;
        }

        try
        {
            var selectedPath = Path.GetFullPath(DatabasePath.Trim());
            await databasePathSettingsService.ConfigureDatabasePathAsync(selectedPath, cancellationToken);
            DatabasePath = selectedPath;
            _savedPath = selectedPath;
            HasUnsavedChanges = false;
            _exitConfirmationPending = false;
            RestartRequired = !string.Equals(
                selectedPath,
                ActiveDatabasePath,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            StatusMessage = RestartRequired
                ? "Database path saved. Close and reopen the application to use it."
                : "The configured database path is already active.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"Could not save database path: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (HasUnsavedChanges && !_exitConfirmationPending)
        {
            _exitConfirmationPending = true;
            StatusMessage = "Unsaved changes will be discarded. Select the exit button again to discard them.";
            return;
        }

        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SelectDatabasePath(string path)
    {
        DatabasePath = path;
        StatusMessage = string.Empty;
    }

    public void ReportBrowseError(string message) =>
        StatusMessage = $"Could not select a database path: {message}";

    partial void OnDatabasePathChanged(string value)
    {
        HasUnsavedChanges = !string.Equals(value, _savedPath, StringComparison.Ordinal);
        _exitConfirmationPending = false;
    }
}
