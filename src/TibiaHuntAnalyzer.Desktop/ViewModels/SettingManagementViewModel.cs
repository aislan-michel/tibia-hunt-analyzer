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

public partial class SettingManagementViewModel(
    ISettingsService settingsService,
    ManagedSettingKind kind) : ViewModelBase
{
    private bool _exitConfirmationPending;

    public ObservableCollection<SettingEntryViewModel> Entries { get; } = [];

    public string Title => kind == ManagedSettingKind.Character
        ? "Gerenciar Personagens"
        : "Gerenciar Locais de Hunt";

    public string NamePlaceholder => kind == ManagedSettingKind.Character
        ? "Nome do personagem"
        : "Nome do local";

    public string ItemLabel => kind == ManagedSettingKind.Character
        ? "personagem"
        : "local de hunt";

    [ObservableProperty]
    public partial string NewName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    public event EventHandler? ExitRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var entries = kind == ManagedSettingKind.Character
            ? await settingsService.GetCharactersAsync(cancellationToken)
            : await settingsService.GetHuntLocationsAsync(cancellationToken);
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(CreateEntry(entry));
        }

        NewName = string.Empty;
        HasUnsavedChanges = false;
        _exitConfirmationPending = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void Add()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            StatusMessage = $"Enter a {ItemLabel} name before adding it.";
            return;
        }

        Entries.Add(CreateEntry(new SettingItem(null, NewName.Trim(), true)));
        NewName = string.Empty;
        MarkChanged();
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var values = Entries.Select(entry => entry.ToSettingItem()).ToArray();
            var request = kind == ManagedSettingKind.Character
                ? new SaveSettingsRequest(values, [])
                : new SaveSettingsRequest([], values);
            await settingsService.SaveAsync(request, cancellationToken);
            await LoadAsync(cancellationToken);
            StatusMessage = $"{Title} saved successfully.";
        }
        catch (ValidationException exception)
        {
            StatusMessage = string.Join(
                Environment.NewLine,
                exception.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusMessage = $"Could not save {Title.ToLowerInvariant()}: {exception.Message}";
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (HasUnsavedChanges && !_exitConfirmationPending)
        {
            _exitConfirmationPending = true;
            StatusMessage = "Unsaved changes will be discarded. Select Voltar again to discard them.";
            return;
        }

        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private SettingEntryViewModel CreateEntry(SettingItem item)
    {
        var entry = new SettingEntryViewModel(item);
        entry.PropertyChanged += (_, _) => MarkChanged();
        return entry;
    }

    private void MarkChanged()
    {
        HasUnsavedChanges = true;
        _exitConfirmationPending = false;
    }

    partial void OnNewNameChanged(string value) => MarkChanged();
}
