namespace TibiaHuntAnalyzer.Application.Analytics;

public interface IHuntAnalyticsService
{
    Task<HuntAnalyticsReport> GetReportAsync(
        HuntAnalyticsFilter? filter = null,
        CancellationToken cancellationToken = default);
}
