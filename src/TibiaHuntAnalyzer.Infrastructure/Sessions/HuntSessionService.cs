using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Application.Imports;
using TibiaHuntAnalyzer.Application.Sessions;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Sessions;

public sealed class HuntSessionService(
    IDbContextFactory<HuntAnalyzerDbContext> dbContextFactory,
    IValidator<SaveHuntSessionRequest> validator) : IHuntSessionService
{
    public async Task<Guid> SaveAsync(
        SaveHuntSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var character = await dbContext.Characters.SingleOrDefaultAsync(
            candidate => candidate.Id == request.CharacterId && candidate.IsActive,
            cancellationToken)
            ?? throw new InvalidOperationException("The selected character does not exist or is inactive.");

        var huntLocation = await dbContext.HuntLocations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.HuntLocationId && candidate.IsActive,
            cancellationToken)
            ?? throw new InvalidOperationException("The selected hunt location does not exist or is inactive.");

        var parsed = request.ParsedSession;
        var session = new HuntSession(
            character,
            huntLocation,
            request.CharacterLevel,
            parsed.SessionStart!.Value,
            parsed.SessionEnd!.Value,
            parsed.Duration!.Value,
            parsed.Statistics,
            parsed.KilledMonsters
                .Select(monster => new KilledMonster(monster.Name, monster.Count)),
            parsed.LootedItems
                .Select(item => new LootedItem(item.Name, item.Quantity)),
            parsed.SourceFormat switch
            {
                HuntInputFormat.Json => SessionInputFormat.Json,
                HuntInputFormat.Text => SessionInputFormat.Text,
                _ => throw new ArgumentOutOfRangeException(nameof(parsed.SourceFormat))
            },
            parsed.OriginalContent);

        dbContext.HuntSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return session.Id;
    }

    public async Task<HuntSessionDetails?> GetDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await dbContext.HuntSessions
            .AsNoTracking()
            .Include(candidate => candidate.Character)
            .Include(candidate => candidate.HuntLocation)
            .Include(candidate => candidate.KilledMonsters)
            .Include(candidate => candidate.LootedItems)
            .SingleOrDefaultAsync(candidate => candidate.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        return new HuntSessionDetails(
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
            session.KilledMonsters
                .Select(monster => new SessionRecord(monster.Name, monster.Count))
                .ToArray(),
            session.LootedItems
                .Select(item => new SessionRecord(item.Name, item.Quantity))
                .ToArray());
    }

    public async Task<IReadOnlyList<HuntSessionSummary>> GetSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await dbContext.HuntSessions
                .AsNoTracking()
                .Select(session => new HuntSessionSummary(
                    session.Id,
                    session.Character.Name,
                    session.CharacterLevel,
                    session.HuntLocation.Name,
                    session.SessionStart,
                    session.SessionEnd))
                .ToListAsync(cancellationToken);

        return sessions
                .OrderByDescending(session => session.SessionStart)
                .ThenBy(session => session.CharacterName)
                .ToArray();
    }
}
