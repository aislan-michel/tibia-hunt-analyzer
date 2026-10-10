namespace TibiaHuntAnalyzer.Application.Analytics;

public sealed record HuntAnalyticsReport(
    IReadOnlyList<HuntSessionAnalysis> Sessions,
    AnalyticsAggregate Overall,
    IReadOnlyList<AnalyticsGroupAggregate> ByCharacter,
    IReadOnlyList<AnalyticsGroupAggregate> ByHuntLocation,
    IReadOnlyList<CharacterLevelPoint> LevelProgression);

public sealed record AnalyticsGroupAggregate(
    Guid Id,
    string Name,
    AnalyticsAggregate Metrics);

public sealed record AnalyticsAggregate(
    int SessionCount,
    AnalyticsMetric Balance,
    AnalyticsMetric BalancePerHour,
    AnalyticsMetric XpGain,
    AnalyticsMetric ReportedXpPerHour,
    AnalyticsMetric CalculatedXpPerHour,
    AnalyticsMetric RawXpGain,
    AnalyticsMetric ReportedRawXpPerHour,
    AnalyticsMetric CalculatedRawXpPerHour);

public sealed record AnalyticsMetric(
    int ContributingSessionCount,
    decimal? Total,
    decimal? Average);

public sealed record CharacterLevelPoint(
    Guid CharacterId,
    string CharacterName,
    Guid SessionId,
    int CharacterLevel,
    DateTimeOffset SessionStart);
