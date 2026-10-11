using System;
using CommunityToolkit.Mvvm.Input;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public sealed partial class HomeViewModel : ViewModelBase
{
    public event EventHandler? InsertRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? QueryRequested;

    public event EventHandler? AnalyticsRequested;

    public event EventHandler? BackupRestoreRequested;

    public event EventHandler? ManageCharactersRequested;

    public event EventHandler? ManageHuntLocationsRequested;

    [RelayCommand]
    private void Insert() => InsertRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Query() => QueryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenAnalytics() => AnalyticsRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenBackupRestore() => BackupRestoreRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ManageCharacters() => ManageCharactersRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ManageHuntLocations() => ManageHuntLocationsRequested?.Invoke(this, EventArgs.Empty);
}
