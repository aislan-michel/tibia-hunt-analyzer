using System;
using CommunityToolkit.Mvvm.Input;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class DatabaseRecoveryViewModel(string errorMessage) : ViewModelBase
{
    public string ErrorMessage { get; } = errorMessage;

    public event EventHandler? ConfigureDatabaseRequested;

    public event EventHandler? ExitRequested;

    [RelayCommand]
    private void ConfigureDatabase() =>
        ConfigureDatabaseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);
}
