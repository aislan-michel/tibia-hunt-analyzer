using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class SettingsViewModel(ISettingsService settingsService) : ViewModelBase
{
    private bool _exitConfirmationPending;

    public ObservableCollection<SettingEntryViewModel> Characters { get; } = [];

    public ObservableCollection<SettingEntryViewModel> HuntLocations { get; } = [];

    [ObservableProperty]
    public partial string NewCharacterName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewHuntLocationName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    public event EventHandler? ExitRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var characters = await settingsService.GetCharactersAsync(cancellationToken);
        var huntLocations = await settingsService.GetHuntLocationsAsync(cancellationToken);

        Characters.Clear();
        foreach (var character in characters)
        {
            Characters.Add(CreateEntry(character));
        }

        HuntLocations.Clear();
        foreach (var location in huntLocations)
        {
            HuntLocations.Add(CreateEntry(location));
        }

        NewCharacterName = string.Empty;
        NewHuntLocationName = string.Empty;
        HasUnsavedChanges = false;
        _exitConfirmationPending = false;
    }

    [RelayCommand]
    private void AddCharacter()
    {
        AddSetting(Characters, NewCharacterName, "character", value => NewCharacterName = value);
    }

    [RelayCommand]
    private void AddHuntLocation()
    {
        AddSetting(HuntLocations, NewHuntLocationName, "hunt location", value => NewHuntLocationName = value);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = new SaveSettingsRequest(
                Characters.Select(character => character.ToSettingItem()).ToArray(),
                HuntLocations.Select(location => location.ToSettingItem()).ToArray());

            await settingsService.SaveAsync(request, cancellationToken);
            StatusMessage = "Settings saved successfully.";
            await LoadAsync(cancellationToken);
        }
        catch (ValidationException exception)
        {
            StatusMessage = string.Join(
                Environment.NewLine,
                exception.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"Could not save settings: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (HasUnsavedChanges && !_exitConfirmationPending)
        {
            _exitConfirmationPending = true;
            StatusMessage = "Unsaved changes will be discarded. Select Sair again to discard them.";
            return;
        }

        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddSetting(
        ObservableCollection<SettingEntryViewModel> entries,
        string name,
        string settingType,
        Action<string> clearName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = $"Enter a {settingType} name before adding it.";
            return;
        }

        entries.Add(CreateEntry(new SettingItem(null, name.Trim(), true)));
        clearName(string.Empty);
        HasUnsavedChanges = true;
        _exitConfirmationPending = false;
        StatusMessage = string.Empty;
    }

    private SettingEntryViewModel CreateEntry(SettingItem item)
    {
        var entry = new SettingEntryViewModel(item);
        entry.PropertyChanged += (_, _) =>
        {
            HasUnsavedChanges = true;
            _exitConfirmationPending = false;
        };

        return entry;
    }

    partial void OnNewCharacterNameChanged(string value)
    {
        HasUnsavedChanges = true;
        _exitConfirmationPending = false;
    }

    partial void OnNewHuntLocationNameChanged(string value)
    {
        HasUnsavedChanges = true;
        _exitConfirmationPending = false;
    }
}
