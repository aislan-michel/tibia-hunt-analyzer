namespace TibiaHuntAnalyzer.Application.Analytics;

public sealed record HuntAnalyticsFilter(
    Guid? CharacterId = null,
    Guid? HuntLocationId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);
