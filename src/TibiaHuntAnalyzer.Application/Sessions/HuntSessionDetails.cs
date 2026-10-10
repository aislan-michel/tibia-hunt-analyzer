using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Application.Sessions;

public sealed record HuntSessionDetails(
    Guid Id,
    string CharacterName,
    Guid CharacterId,
    int CharacterLevel,
    string HuntLocationName,
    Guid HuntLocationId,
    DateTimeOffset SessionStart,
    DateTimeOffset SessionEnd,
    TimeSpan Duration,
    TimeSpan ElapsedTime,
    SessionInputFormat InputFormat,
    string OriginalInput,
    HuntStatistics Statistics,
    IReadOnlyList<SessionRecord> KilledMonsters,
    IReadOnlyList<SessionRecord> LootedItems);
