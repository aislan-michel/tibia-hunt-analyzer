using System.Collections.ObjectModel;

namespace TibiaHuntAnalyzer.Domain;

public sealed class HuntSession
{
    public HuntSession(
        Character character,
        HuntLocation huntLocation,
        int characterLevel,
        DateTimeOffset sessionStart,
        DateTimeOffset sessionEnd,
        TimeSpan duration,
        HuntStatistics statistics,
        IEnumerable<KilledMonster>? killedMonsters = null,
        IEnumerable<LootedItem>? lootedItems = null)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(huntLocation);
        ArgumentNullException.ThrowIfNull(statistics);

        if (characterLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(characterLevel), "Character level must be positive.");
        }

        if (sessionEnd < sessionStart)
        {
            throw new ArgumentException("Session end cannot be earlier than session start.", nameof(sessionEnd));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Session duration cannot be negative.");
        }

        Id = Guid.NewGuid();
        Character = character;
        HuntLocation = huntLocation;
        CharacterLevel = characterLevel;
        SessionStart = sessionStart;
        SessionEnd = sessionEnd;
        Duration = duration;
        Statistics = statistics;
        KilledMonsters = CreateCollection(killedMonsters);
        LootedItems = CreateCollection(lootedItems);

        EnsureUniqueAndUnattached(
            KilledMonsters,
            monster => monster.Id,
            monster => monster.EnsureCanAttachToSession());
        EnsureUniqueAndUnattached(
            LootedItems,
            item => item.Id,
            item => item.EnsureCanAttachToSession());

        foreach (var monster in KilledMonsters)
        {
            monster.AttachToSession(Id);
        }

        foreach (var item in LootedItems)
        {
            item.AttachToSession(Id);
        }
    }

    public Guid Id { get; private set; }

    public Character Character { get; private set; }

    public HuntLocation HuntLocation { get; private set; }

    public int CharacterLevel { get; private set; }

    public DateTimeOffset SessionStart { get; private set; }

    public DateTimeOffset SessionEnd { get; private set; }

    public TimeSpan Duration { get; private set; }

    public TimeSpan ElapsedTime => SessionEnd - SessionStart;

    public HuntStatistics Statistics { get; private set; }

    public IReadOnlyList<KilledMonster> KilledMonsters { get; private set; }

    public IReadOnlyList<LootedItem> LootedItems { get; private set; }

    private static IReadOnlyList<T> CreateCollection<T>(IEnumerable<T>? values)
    {
        var items = values?.ToArray() ?? [];
        if (items.Any(item => item is null))
        {
            throw new ArgumentException("Session records cannot contain null entries.", nameof(values));
        }

        return new ReadOnlyCollection<T>(items);
    }

    private static void EnsureUniqueAndUnattached<T>(
        IReadOnlyList<T> records,
        Func<T, Guid> getId,
        Action<T> ensureCanAttach)
    {
        var seen = new HashSet<Guid>();
        foreach (var record in records)
        {
            if (!seen.Add(getId(record)))
            {
                throw new ArgumentException("The same record instance cannot appear more than once.", nameof(records));
            }

            ensureCanAttach(record);
        }
    }
}
