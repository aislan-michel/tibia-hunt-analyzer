using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Sessions;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public enum SessionSortField
{
    Start,
    Character,
    Level,
    HuntLocation,
    End
}

public partial class SessionQueryViewModel(
    IHuntSessionService huntSessionService) : ViewModelBase
{
    private HuntSessionSummary[] _allSessions = [];

    public ObservableCollection<HuntSessionSummary> Sessions { get; } = [];

    public ObservableCollection<SessionRecord> DetailMonsters { get; } = [];

    public ObservableCollection<SessionRecord> DetailItems { get; } = [];

    [ObservableProperty]
    public partial string CharacterFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HuntLocationFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartDateFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EndDateFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SessionSortField SortField { get; set; } = SessionSortField.Start;

    [ObservableProperty]
    public partial bool SortDescending { get; set; } = true;

    [ObservableProperty]
    public partial HuntSessionSummary? SelectedSession { get; set; }

    [ObservableProperty]
    public partial HuntSessionDetails? SelectedDetails { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDetailsVisible { get; private set; }

    public event EventHandler? ExitRequested;

    public async Task LoadAsync()
    {
        try
        {
            _allSessions = (await huntSessionService.GetSummariesAsync()).ToArray();
            StatusMessage = string.Empty;
            ApplyFilters();
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not load hunt sessions: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task OpenDetailsAsync()
    {
        if (SelectedSession is null)
        {
            StatusMessage = "Select a session to view its details.";
            return;
        }

        try
        {
            var details = await huntSessionService.GetDetailsAsync(SelectedSession.Id);
            if (details is null)
            {
                StatusMessage = "The selected session could not be found.";
                return;
            }

            SelectedDetails = details;
            DetailMonsters.Clear();
            foreach (var monster in details.KilledMonsters)
            {
                DetailMonsters.Add(monster);
            }

            DetailItems.Clear();
            foreach (var item in details.LootedItems)
            {
                DetailItems.Add(item);
            }

            IsDetailsVisible = true;
            StatusMessage = string.Empty;
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not load session details: {exception.Message}";
        }
    }

    [RelayCommand]
    private void BackToSessions()
    {
        IsDetailsVisible = false;
        SelectedDetails = null;
        DetailMonsters.Clear();
        DetailItems.Clear();
    }

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ClearFilters()
    {
        CharacterFilter = string.Empty;
        HuntLocationFilter = string.Empty;
        StartDateFilter = string.Empty;
        EndDateFilter = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void SortBy(SessionSortField field)
    {
        if (SortField == field)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortField = field;
            SortDescending = field is SessionSortField.Start or SessionSortField.End;
        }

        ApplyFilters();
    }

    partial void OnCharacterFilterChanged(string value) => ApplyFilters();

    partial void OnHuntLocationFilterChanged(string value) => ApplyFilters();

    partial void OnStartDateFilterChanged(string value) => ApplyFilters();

    partial void OnEndDateFilterChanged(string value) => ApplyFilters();

    partial void OnSortFieldChanged(SessionSortField value) => ApplyFilters();

    partial void OnSortDescendingChanged(bool value) => ApplyFilters();

    private void ApplyFilters()
    {
        if (IsInvalidDate(StartDateFilter) || IsInvalidDate(EndDateFilter))
        {
            StatusMessage = "Enter dates in YYYY-MM-DD format.";
            Sessions.Clear();
            return;
        }

        var startDate = ParseDate(StartDateFilter);
        var endDate = ParseDate(EndDateFilter);
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            StatusMessage = "Start date must not be later than end date.";
            Sessions.Clear();
            return;
        }

        var filtered = _allSessions.Where(session =>
                Contains(session.CharacterName, CharacterFilter) &&
                Contains(session.HuntLocationName, HuntLocationFilter) &&
                (!startDate.HasValue || DateOnly.FromDateTime(session.SessionStart.LocalDateTime) >= startDate) &&
                (!endDate.HasValue || DateOnly.FromDateTime(session.SessionStart.LocalDateTime) <= endDate))
            .ToArray();

        var sorted = SortField switch
        {
            SessionSortField.Character => SortDescending
                ? filtered.OrderByDescending(session => session.CharacterName, StringComparer.CurrentCultureIgnoreCase)
                : filtered.OrderBy(session => session.CharacterName, StringComparer.CurrentCultureIgnoreCase),
            SessionSortField.Level => SortDescending
                ? filtered.OrderByDescending(session => session.CharacterLevel)
                : filtered.OrderBy(session => session.CharacterLevel),
            SessionSortField.HuntLocation => SortDescending
                ? filtered.OrderByDescending(session => session.HuntLocationName, StringComparer.CurrentCultureIgnoreCase)
                : filtered.OrderBy(session => session.HuntLocationName, StringComparer.CurrentCultureIgnoreCase),
            SessionSortField.End => SortDescending
                ? filtered.OrderByDescending(session => session.SessionEnd)
                : filtered.OrderBy(session => session.SessionEnd),
            _ => SortDescending
                ? filtered.OrderByDescending(session => session.SessionStart)
                : filtered.OrderBy(session => session.SessionStart)
        };

        Sessions.Clear();
        foreach (var session in sorted)
        {
            Sessions.Add(session);
        }

        StatusMessage = _allSessions.Length == 0
            ? "No hunt sessions have been recorded yet."
            : $"Showing {Sessions.Count} of {_allSessions.Length} sessions.";
    }

    private static bool Contains(string value, string filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        value.Contains(filter.Trim(), StringComparison.CurrentCultureIgnoreCase);

    private static bool IsInvalidDate(string value) =>
        !string.IsNullOrWhiteSpace(value) && !ParseDate(value).HasValue;

    private static DateOnly? ParseDate(string value) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
}
