using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Application.Analytics;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Analytics;

public sealed class HuntAnalyticsService(
    IDbContextFactory<HuntAnalyzerDbContext> dbContextFactory) : IHuntAnalyticsService
{
    public async Task<HuntAnalyticsReport> GetReportAsync(
        HuntAnalyticsFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new HuntAnalyticsFilter();
        if (filter.StartDate.HasValue &&
            filter.EndDate.HasValue &&
            filter.StartDate > filter.EndDate)
        {
            throw new ArgumentException("Start date must not be later than end date.", nameof(filter));
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.HuntSessions
            .AsNoTracking()
            .AsQueryable();

        if (filter.CharacterId.HasValue)
        {
            query = query.Where(session => session.CharacterId == filter.CharacterId.Value);
        }

        if (filter.HuntLocationId.HasValue)
        {
            query = query.Where(session => session.HuntLocationId == filter.HuntLocationId.Value);
        }

        var sessions = await query
            .Include(session => session.Character)
            .Include(session => session.HuntLocation)
            .ToListAsync(cancellationToken);

        var details = sessions.Select(session => new HuntSessionDetails(
            session.Id,
            session.Character.Name,
            session.CharacterId,
            session.CharacterLevel,
            session.HuntLocation.Name,
            session.HuntLocationId,
            session.SessionStart,
            session.SessionEnd,
            session.Duration,
            session.ElapsedTime,
            session.InputFormat,
            session.OriginalInput,
            session.Statistics,
            [],
            [])).ToArray();

        return HuntAnalyticsCalculator.Calculate(details, filter);
    }
}
