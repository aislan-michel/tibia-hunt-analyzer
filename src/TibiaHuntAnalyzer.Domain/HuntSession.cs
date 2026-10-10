namespace TibiaHuntAnalyzer.Domain;

public sealed class HuntSession
{
    private readonly List<KilledMonster> _killedMonsters = [];
    private readonly List<LootedItem> _lootedItems = [];

    private HuntSession()
    {
        Character = null!;
        HuntLocation = null!;
        Statistics = null!;
        OriginalInput = string.Empty;
    }

    public HuntSession(
        Character character,
        HuntLocation huntLocation,
        int characterLevel,
        DateTimeOffset sessionStart,
        DateTimeOffset sessionEnd,
        TimeSpan duration,
        HuntStatistics statistics,
        IEnumerable<KilledMonster>? killedMonsters = null,
        IEnumerable<LootedItem>? lootedItems = null,
        SessionInputFormat inputFormat = SessionInputFormat.Json,
        string originalInput = "")
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
        CharacterId = character.Id;
        HuntLocation = huntLocation;
        HuntLocationId = huntLocation.Id;
        CharacterLevel = characterLevel;
        SessionStart = sessionStart;
        SessionEnd = sessionEnd;
        Duration = duration;
        Statistics = statistics;
        InputFormat = inputFormat;
        ArgumentNullException.ThrowIfNull(originalInput);
        OriginalInput = originalInput;
        var monsters = CreateCollection(killedMonsters);
        var items = CreateCollection(lootedItems);

        EnsureUniqueAndUnattached(
            monsters,
            monster => monster.Id,
            monster => monster.EnsureCanAttachToSession());
        EnsureUniqueAndUnattached(
            items,
            item => item.Id,
            item => item.EnsureCanAttachToSession());

        foreach (var monster in monsters)
        {
            monster.AttachToSession(Id);
            _killedMonsters.Add(monster);
        }

        foreach (var item in items)
        {
            item.AttachToSession(Id);
            _lootedItems.Add(item);
        }
    }

    public Guid Id { get; private set; }

    public Character Character { get; private set; }

    public Guid CharacterId { get; private set; }

    public HuntLocation HuntLocation { get; private set; }

    public Guid HuntLocationId { get; private set; }

    public int CharacterLevel { get; private set; }

    public DateTimeOffset SessionStart { get; private set; }

    public DateTimeOffset SessionEnd { get; private set; }

    public TimeSpan Duration { get; private set; }

    public TimeSpan ElapsedTime => SessionEnd - SessionStart;

    public HuntStatistics Statistics { get; private set; }

    public SessionInputFormat InputFormat { get; private set; }

    public string OriginalInput { get; private set; }

    public IReadOnlyList<KilledMonster> KilledMonsters => _killedMonsters;

    public IReadOnlyList<LootedItem> LootedItems => _lootedItems;

    private static T[] CreateCollection<T>(IEnumerable<T>? values)
    {
        var items = values?.ToArray() ?? [];
        if (items.Any(item => item is null))
        {
            throw new ArgumentException("Session records cannot contain null entries.", nameof(values));
        }

        return items;
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
