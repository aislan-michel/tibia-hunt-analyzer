namespace TibiaHuntAnalyzer.Domain;

public sealed class KilledMonster
{
    private KilledMonster()
    {
        Name = null!;
    }

    public KilledMonster(string name, long? count)
    {
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Count = ValidateCount(count);
    }

    public Guid Id { get; private set; }

    public Guid HuntSessionId { get; private set; }

    public string Name { get; private set; }

    public long? Count { get; private set; }

    internal void EnsureCanAttachToSession()
    {
        if (HuntSessionId != Guid.Empty)
        {
            throw new InvalidOperationException("The killed monster already belongs to a hunt session.");
        }
    }

    internal void AttachToSession(Guid huntSessionId)
    {
        if (huntSessionId == Guid.Empty)
        {
            throw new ArgumentException("A hunt session ID is required.", nameof(huntSessionId));
        }

        EnsureCanAttachToSession();

        HuntSessionId = huntSessionId;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A monster name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static long? ValidateCount(long? count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "A monster count must be positive when provided.");
        }

        return count;
    }
}
