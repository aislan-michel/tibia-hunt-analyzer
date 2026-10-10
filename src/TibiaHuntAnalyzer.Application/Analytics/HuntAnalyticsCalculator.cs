using TibiaHuntAnalyzer.Application.Sessions;

namespace TibiaHuntAnalyzer.Application.Analytics;

public static class HuntAnalyticsCalculator
{
    public static HuntAnalyticsReport Calculate(
        IEnumerable<HuntSessionDetails> sessions,
        HuntAnalyticsFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        filter ??= new HuntAnalyticsFilter();

        if (filter.StartDate.HasValue &&
            filter.EndDate.HasValue &&
            filter.StartDate > filter.EndDate)
        {
            throw new ArgumentException("Start date must not be later than end date.", nameof(filter));
        }

        var analyzed = sessions
            .Where(session =>
                (!filter.CharacterId.HasValue || session.CharacterId == filter.CharacterId) &&
                (!filter.HuntLocationId.HasValue || session.HuntLocationId == filter.HuntLocationId) &&
                (!filter.StartDate.HasValue ||
                 DateOnly.FromDateTime(session.SessionStart.LocalDateTime) >= filter.StartDate) &&
                (!filter.EndDate.HasValue ||
                 DateOnly.FromDateTime(session.SessionStart.LocalDateTime) <= filter.EndDate))
            .Select(AnalyzeSession)
            .OrderBy(session => session.SessionStart)
            .ThenBy(session => session.SessionId)
            .ToArray();

        return new HuntAnalyticsReport(
            analyzed,
            Aggregate(analyzed),
            analyzed
                .GroupBy(session => new { session.CharacterId, session.CharacterName })
                .Select(group => new AnalyticsGroupAggregate(
                    group.Key.CharacterId,
                    group.Key.CharacterName,
                    Aggregate(group)))
                .OrderBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            analyzed
                .GroupBy(session => new { session.HuntLocationId, session.HuntLocationName })
                .Select(group => new AnalyticsGroupAggregate(
                    group.Key.HuntLocationId,
                    group.Key.HuntLocationName,
                    Aggregate(group)))
                .OrderBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            analyzed
                .Select(session => new CharacterLevelPoint(
                    session.CharacterId,
                    session.CharacterName,
                    session.SessionId,
                    session.CharacterLevel,
                    session.SessionStart))
                .OrderBy(point => point.CharacterName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(point => point.SessionStart)
                .ThenBy(point => point.SessionId)
                .ToArray());
    }

    private static HuntSessionAnalysis AnalyzeSession(HuntSessionDetails session)
    {
        var durationHours = session.Duration.TotalHours;
        var hasPositiveDuration = durationHours > 0;
        var statistics = session.Statistics;

        return new HuntSessionAnalysis(
            session.Id,
            session.CharacterId,
            session.CharacterName,
            session.CharacterLevel,
            session.HuntLocationId,
            session.HuntLocationName,
            session.SessionStart,
            session.SessionEnd,
            session.Duration,
            statistics.Balance,
            hasPositiveDuration && statistics.Balance.HasValue
                ? statistics.Balance.Value / (decimal)durationHours
                : null,
            statistics.XpGain,
            statistics.XpPerHour,
            CalculatePerHour(statistics.XpGain, durationHours),
            statistics.RawXpGain,
            statistics.RawXpPerHour,
            CalculatePerHour(statistics.RawXpGain, durationHours));
    }

    private static decimal? CalculatePerHour(long? amount, double durationHours) =>
        amount.HasValue && durationHours > 0
            ? amount.Value / (decimal)durationHours
            : null;

    private static AnalyticsAggregate Aggregate(IEnumerable<HuntSessionAnalysis> sessions)
    {
        var rows = sessions.ToArray();
        return new AnalyticsAggregate(
            rows.Length,
            CreateMetric(rows.Select(session => session.Balance)),
            CreateMetric(rows.Select(session => session.CalculatedBalancePerHour), sumValues: false),
            CreateMetric(rows.Select(session => ToDecimal(session.XpGain))),
            CreateMetric(rows.Select(session => session.ReportedXpPerHour), sumValues: false),
            CreateMetric(rows.Select(session => session.CalculatedXpPerHour), sumValues: false),
            CreateMetric(rows.Select(session => ToDecimal(session.RawXpGain))),
            CreateMetric(rows.Select(session => session.ReportedRawXpPerHour), sumValues: false),
            CreateMetric(rows.Select(session => session.CalculatedRawXpPerHour), sumValues: false));
    }

    private static AnalyticsMetric CreateMetric(IEnumerable<decimal?> values, bool sumValues = true)
    {
        var presentValues = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return new AnalyticsMetric(
            presentValues.Length,
            sumValues && presentValues.Length > 0 ? presentValues.Sum() : null,
            presentValues.Length > 0 ? presentValues.Average() : null);
    }

    private static decimal? ToDecimal(long? value) =>
        value.HasValue ? value.Value : null;
}
