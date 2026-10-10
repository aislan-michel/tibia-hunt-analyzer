using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Settings;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public partial class AnalyticsViewModel(
    IHuntAnalyticsService analyticsService,
    ISettingsService settingsService) : ViewModelBase
{
    public ObservableCollection<AnalyticsFilterOption> Characters { get; } = [];

    public ObservableCollection<AnalyticsFilterOption> HuntLocations { get; } = [];

    public ObservableCollection<AnalyticsGroupDisplay> CharacterResults { get; } = [];

    public ObservableCollection<AnalyticsGroupDisplay> HuntLocationResults { get; } = [];

    public ObservableCollection<CharacterLevelDisplay> LevelProgression { get; } = [];

    [ObservableProperty]
    public partial AnalyticsFilterOption? SelectedCharacter { get; set; }

    [ObservableProperty]
    public partial AnalyticsFilterOption? SelectedHuntLocation { get; set; }

    [ObservableProperty]
    public partial string StartDateFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EndDateFilter { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SessionCount { get; private set; }

    [ObservableProperty]
    public partial string BalanceTotal { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageBalance { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageBalancePerHour { get; private set; } = "—";

    [ObservableProperty]
    public partial string XpTotal { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageReportedXpPerHour { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageCalculatedXpPerHour { get; private set; } = "—";

    [ObservableProperty]
    public partial string RawXpTotal { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageReportedRawXpPerHour { get; private set; } = "—";

    [ObservableProperty]
    public partial string AverageCalculatedRawXpPerHour { get; private set; } = "—";

    public event EventHandler? ExitRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var characters = await settingsService.GetCharactersAsync(cancellationToken);
            var locations = await settingsService.GetHuntLocationsAsync(cancellationToken);
            Characters.Clear();
            Characters.Add(new AnalyticsFilterOption(null, "All characters"));
            foreach (var character in characters.Where(item => item.IsActive).OrderBy(item => item.Name))
            {
                Characters.Add(new AnalyticsFilterOption(character.Id, character.Name));
            }

            HuntLocations.Clear();
            HuntLocations.Add(new AnalyticsFilterOption(null, "All hunt locations"));
            foreach (var location in locations.Where(item => item.IsActive).OrderBy(item => item.Name))
            {
                HuntLocations.Add(new AnalyticsFilterOption(location.Id, location.Name));
            }

            SelectedCharacter = Characters[0];
            SelectedHuntLocation = HuntLocations[0];
            await AnalyzeAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not load analytics: {exception.Message}";
        }
    }

    [RelayCommand]
    private async Task AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        if (!TryParseDate(StartDateFilter, out var startDate) ||
            !TryParseDate(EndDateFilter, out var endDate))
        {
            StatusMessage = "Enter dates in YYYY-MM-DD format.";
            return;
        }

        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            StatusMessage = "Start date must not be later than end date.";
            return;
        }

        try
        {
            var report = await analyticsService.GetReportAsync(
                new HuntAnalyticsFilter(
                    SelectedCharacter?.Id,
                    SelectedHuntLocation?.Id,
                    startDate,
                    endDate),
                cancellationToken);

            SessionCount = report.Overall.SessionCount;
            BalanceTotal = AnalyticsGroupDisplay.FormatMetric(report.Overall.Balance, useTotal: true);
            AverageBalance = AnalyticsGroupDisplay.FormatMetric(report.Overall.Balance, useTotal: false);
            AverageBalancePerHour = AnalyticsGroupDisplay.FormatMetric(report.Overall.BalancePerHour, useTotal: false);
            XpTotal = AnalyticsGroupDisplay.FormatMetric(report.Overall.XpGain, useTotal: true);
            AverageReportedXpPerHour = AnalyticsGroupDisplay.FormatMetric(report.Overall.ReportedXpPerHour, useTotal: false);
            AverageCalculatedXpPerHour = AnalyticsGroupDisplay.FormatMetric(report.Overall.CalculatedXpPerHour, useTotal: false);
            RawXpTotal = AnalyticsGroupDisplay.FormatMetric(report.Overall.RawXpGain, useTotal: true);
            AverageReportedRawXpPerHour = AnalyticsGroupDisplay.FormatMetric(report.Overall.ReportedRawXpPerHour, useTotal: false);
            AverageCalculatedRawXpPerHour = AnalyticsGroupDisplay.FormatMetric(report.Overall.CalculatedRawXpPerHour, useTotal: false);

            ReplaceResults(CharacterResults, report.ByCharacter.Select(AnalyticsGroupDisplay.Create));
            ReplaceResults(HuntLocationResults, report.ByHuntLocation.Select(AnalyticsGroupDisplay.Create));
            ReplaceResults(LevelProgression, report.LevelProgression.Select(CharacterLevelDisplay.Create));
            StatusMessage = report.Overall.SessionCount == 0
                ? "No sessions match the selected filters."
                : $"Analytics calculated from {report.Overall.SessionCount} session(s).";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not calculate analytics: {exception.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        StartDateFilter = string.Empty;
        EndDateFilter = string.Empty;
        SelectedCharacter = Characters.FirstOrDefault();
        SelectedHuntLocation = HuntLocations.FirstOrDefault();
    }

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    private static bool TryParseDate(string value, out DateOnly? date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = null;
            return true;
        }

        if (DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            date = parsed;
            return true;
        }

        date = null;
        return false;
    }

    private static void ReplaceResults<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
