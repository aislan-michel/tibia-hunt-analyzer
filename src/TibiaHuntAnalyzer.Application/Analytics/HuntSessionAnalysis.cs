namespace TibiaHuntAnalyzer.Application.Analytics;

public sealed record HuntSessionAnalysis(
    Guid SessionId,
    Guid CharacterId,
    string CharacterName,
    int CharacterLevel,
    Guid HuntLocationId,
    string HuntLocationName,
    DateTimeOffset SessionStart,
    DateTimeOffset SessionEnd,
    TimeSpan ReportedDuration,
    decimal? Balance,
    decimal? CalculatedBalancePerHour,
    long? XpGain,
    decimal? ReportedXpPerHour,
    decimal? CalculatedXpPerHour,
    long? RawXpGain,
    decimal? ReportedRawXpPerHour,
    decimal? CalculatedRawXpPerHour);
