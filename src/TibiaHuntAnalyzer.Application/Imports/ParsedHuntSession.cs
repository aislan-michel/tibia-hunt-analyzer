using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Application.Imports;

public sealed class ParsedHuntSession(
    HuntInputFormat sourceFormat,
    string originalContent,
    DateTimeOffset? sessionStart,
    DateTimeOffset? sessionEnd,
    TimeSpan? duration,
    HuntStatistics statistics,
    IReadOnlyList<KilledMonster> killedMonsters,
    IReadOnlyList<LootedItem> lootedItems)
{
    public HuntInputFormat SourceFormat { get; } = sourceFormat;

    public string OriginalContent { get; } = originalContent;

    public DateTimeOffset? SessionStart { get; } = sessionStart;

    public DateTimeOffset? SessionEnd { get; } = sessionEnd;

    public TimeSpan? Duration { get; } = duration;

    public HuntStatistics Statistics { get; } = statistics;

    public IReadOnlyList<KilledMonster> KilledMonsters { get; } = killedMonsters;

    public IReadOnlyList<LootedItem> LootedItems { get; } = lootedItems;
}
