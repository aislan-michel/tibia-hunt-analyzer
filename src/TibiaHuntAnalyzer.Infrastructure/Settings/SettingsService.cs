using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Application.Settings;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Settings;

public sealed class SettingsService(
    IDbContextFactory<HuntAnalyzerDbContext> dbContextFactory,
    IValidator<SaveSettingsRequest> validator) : ISettingsService
{
    public async Task<IReadOnlyList<SettingItem>> GetCharactersAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var characters = await dbContext.Characters
            .AsNoTracking()
            .OrderBy(character => character.Name)
            .ToListAsync(cancellationToken);

        return characters
            .Select(character => new SettingItem(character.Id, character.Name, character.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<SettingItem>> GetHuntLocationsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var locations = await dbContext.HuntLocations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        return locations
            .Select(location => new SettingItem(location.Id, location.Name, location.IsActive))
            .ToArray();
    }

    public async Task SaveAsync(
        SaveSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await SaveCharactersAsync(dbContext, request.Characters, cancellationToken);
        await SaveHuntLocationsAsync(dbContext, request.HuntLocations, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task SaveCharactersAsync(
        HuntAnalyzerDbContext dbContext,
        IReadOnlyList<SettingItem> items,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.Characters.ToDictionaryAsync(
            character => character.Id,
            cancellationToken);

        foreach (var item in items)
        {
            if (item.Id is null)
            {
                var newCharacter = new Character(item.Name);
                if (!item.IsActive)
                {
                    newCharacter.Deactivate();
                }

                dbContext.Characters.Add(newCharacter);
                continue;
            }

            if (!entities.TryGetValue(item.Id.Value, out var character))
            {
                throw new InvalidOperationException($"Character '{item.Id}' no longer exists.");
            }

            character.Rename(item.Name);
            SetActiveState(item.IsActive, character.Deactivate, character.Reactivate);
        }
    }

    private async Task SaveHuntLocationsAsync(
        HuntAnalyzerDbContext dbContext,
        IReadOnlyList<SettingItem> items,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.HuntLocations.ToDictionaryAsync(
            location => location.Id,
            cancellationToken);

        foreach (var item in items)
        {
            if (item.Id is null)
            {
                var newLocation = new HuntLocation(item.Name);
                if (!item.IsActive)
                {
                    newLocation.Deactivate();
                }

                dbContext.HuntLocations.Add(newLocation);
                continue;
            }

            if (!entities.TryGetValue(item.Id.Value, out var location))
            {
                throw new InvalidOperationException($"Hunt location '{item.Id}' no longer exists.");
            }

            location.Rename(item.Name);
            SetActiveState(item.IsActive, location.Deactivate, location.Reactivate);
        }
    }

    private static void SetActiveState(bool isActive, Action deactivate, Action reactivate)
    {
        if (isActive)
        {
            reactivate();
        }
        else
        {
            deactivate();
        }
    }
}
