using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentValidation;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class InsertHuntSessionViewModel(
    ISettingsService settingsService,
    IHuntSessionService huntSessionService,
    JsonHuntSessionParser jsonParser,
    TextHuntSessionParser textParser) : ViewModelBase
{
    private PendingConfirmationAction _pendingConfirmation;

    public ObservableCollection<SettingItem> Characters { get; } = [];

    public ObservableCollection<SettingItem> HuntLocations { get; } = [];

    [ObservableProperty]
    public partial SettingItem? SelectedCharacter { get; set; }

    [ObservableProperty]
    public partial SettingItem? SelectedHuntLocation { get; set; }

    [ObservableProperty]
    public partial string CharacterLevelText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedInputFormatIndex { get; set; }

    [ObservableProperty]
    public partial string InputContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasUnsavedInput { get; private set; }

    public bool IsJsonFile => SelectedInputFormatIndex == 0;

    public bool IsTextInput => SelectedInputFormatIndex == 1;

    public event EventHandler? ExitRequested;

    public event EventHandler? FileSelectionRequested;

    public async Task LoadAsync()
    {
        var characters = await settingsService.GetCharactersAsync();
        var locations = await settingsService.GetHuntLocationsAsync();

        Characters.Clear();
        foreach (var character in characters.Where(item => item.IsActive))
        {
            Characters.Add(character);
        }

        HuntLocations.Clear();
        foreach (var location in locations.Where(item => item.IsActive))
        {
            HuntLocations.Add(location);
        }
    }

    [RelayCommand]
    private void SelectJsonFile()
    {
        FileSelectionRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Exit()
    {
        if (HasUnsavedInput && _pendingConfirmation != PendingConfirmationAction.Exit)
        {
            _pendingConfirmation = PendingConfirmationAction.Exit;
            StatusMessage = "Unsaved input will be discarded. Select Sair again to close the application.";
            return;
        }

        _pendingConfirmation = PendingConfirmationAction.None;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Clear()
    {
        if (HasUnsavedInput && _pendingConfirmation != PendingConfirmationAction.Clear)
        {
            _pendingConfirmation = PendingConfirmationAction.Clear;
            StatusMessage = "Unsaved input will be cleared. Select Limpar again to discard it.";
            return;
        }

        SelectedCharacter = null;
        SelectedHuntLocation = null;
        CharacterLevelText = string.Empty;
        SelectedInputFormatIndex = 0;
        InputContent = string.Empty;
        SelectedFileName = string.Empty;
        StatusMessage = string.Empty;
        HasUnsavedInput = false;
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        var errors = ValidateForm();
        if (errors.Count > 0)
        {
            StatusMessage = string.Join(Environment.NewLine, errors);
            return;
        }

        var parseResult = IsJsonFile
            ? jsonParser.Parse(InputContent)
            : textParser.Parse(InputContent);

        if (!parseResult.IsSuccess)
        {
            StatusMessage = string.Join(
                Environment.NewLine,
                parseResult.Errors.Select(error => $"{error.Field}: {error.Message}"));
            return;
        }

        if (!int.TryParse(
                CharacterLevelText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var characterLevel))
        {
            StatusMessage = "Character level must be a positive whole number.";
            return;
        }

        try
        {
            await huntSessionService.SaveAsync(new SaveHuntSessionRequest(
                SelectedCharacter!.Id!.Value,
                SelectedHuntLocation!.Id!.Value,
                characterLevel,
                parseResult.Session!));

            StatusMessage = "Hunt session saved successfully.";
            HasUnsavedInput = false;
            _pendingConfirmation = PendingConfirmationAction.None;
        }
        catch (ValidationException exception)
        {
            StatusMessage = string.Join(
                Environment.NewLine,
                exception.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not save the hunt session: {exception.Message}";
        }
    }

    public async Task LoadJsonFileAsync(string fileName, Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream);
            InputContent = await reader.ReadToEndAsync();
            SelectedFileName = fileName;
            HasUnsavedInput = true;
            _pendingConfirmation = PendingConfirmationAction.None;
            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not read the selected file: {exception.Message}";
        }
    }

    public void ReportFileError(string message)
    {
        StatusMessage = $"Could not open the selected file: {message}";
    }

    partial void OnSelectedInputFormatIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsJsonFile));
        OnPropertyChanged(nameof(IsTextInput));
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    partial void OnSelectedCharacterChanged(SettingItem? value)
    {
        HasUnsavedInput = true;
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    partial void OnSelectedHuntLocationChanged(SettingItem? value)
    {
        HasUnsavedInput = true;
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    partial void OnCharacterLevelTextChanged(string value)
    {
        HasUnsavedInput = true;
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    partial void OnInputContentChanged(string value)
    {
        HasUnsavedInput = true;
        _pendingConfirmation = PendingConfirmationAction.None;
    }

    private System.Collections.Generic.List<string> ValidateForm()
    {
        var errors = new System.Collections.Generic.List<string>();

        if (SelectedCharacter?.Id is null)
        {
            errors.Add("Select a character.");
        }

        if (SelectedHuntLocation?.Id is null)
        {
            errors.Add("Select a hunt location.");
        }

        if (!int.TryParse(
                CharacterLevelText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var level) ||
            level <= 0)
        {
            errors.Add("Character level must be a positive whole number.");
        }

        if (IsJsonFile && string.IsNullOrWhiteSpace(SelectedFileName))
        {
            errors.Add("Select a JSON file.");
        }

        if (string.IsNullOrWhiteSpace(InputContent))
        {
            errors.Add(IsJsonFile ? "The selected JSON file is empty." : "Enter copied Hunt Analyzer text.");
        }

        return errors;
    }

    private enum PendingConfirmationAction
    {
        None,
        Clear,
        Exit
    }
}
