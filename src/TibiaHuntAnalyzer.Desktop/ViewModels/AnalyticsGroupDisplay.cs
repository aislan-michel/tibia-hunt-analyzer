using System.Globalization;
using TibiaHuntAnalyzer.Application.Analytics;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public sealed record AnalyticsGroupDisplay(
    string Name,
    string Sessions,
    string BalanceTotal,
    string AverageBalance,
    string AverageBalancePerHour,
    string XpTotal,
    string AverageReportedXpPerHour,
    string AverageCalculatedXpPerHour,
    string RawXpTotal,
    string AverageReportedRawXpPerHour,
    string AverageCalculatedRawXpPerHour)
{
    public static AnalyticsGroupDisplay Create(AnalyticsGroupAggregate group) =>
        new(
            group.Name,
            group.Metrics.SessionCount.ToString(CultureInfo.CurrentCulture),
            FormatMetric(group.Metrics.Balance, useTotal: true),
            FormatMetric(group.Metrics.Balance, useTotal: false),
            FormatMetric(group.Metrics.BalancePerHour, useTotal: false),
            FormatMetric(group.Metrics.XpGain, useTotal: true),
            FormatMetric(group.Metrics.ReportedXpPerHour, useTotal: false),
            FormatMetric(group.Metrics.CalculatedXpPerHour, useTotal: false),
            FormatMetric(group.Metrics.RawXpGain, useTotal: true),
            FormatMetric(group.Metrics.ReportedRawXpPerHour, useTotal: false),
            FormatMetric(group.Metrics.CalculatedRawXpPerHour, useTotal: false));

    internal static string FormatMetric(AnalyticsMetric metric, bool useTotal) =>
        $"{Format(useTotal ? metric.Total : metric.Average)} ({metric.ContributingSessionCount} session(s))";

    internal static string Format(decimal? value) =>
        value?.ToString("N2", CultureInfo.CurrentCulture) ?? "—";
}
