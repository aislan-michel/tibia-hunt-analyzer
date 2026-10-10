namespace TibiaHuntAnalyzer.Domain;

public sealed class LootedItem
{
    private LootedItem()
    {
        Name = null!;
    }

    public LootedItem(string name, long? quantity)
    {
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Quantity = ValidateQuantity(quantity);
    }

    public Guid Id { get; private set; }

    public Guid HuntSessionId { get; private set; }

    public string Name { get; private set; }

    public long? Quantity { get; private set; }

    internal void EnsureCanAttachToSession()
    {
        if (HuntSessionId != Guid.Empty)
        {
            throw new InvalidOperationException("The looted item already belongs to a hunt session.");
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
            throw new ArgumentException("An item name is required.", nameof(name));
        }

        return name.Trim();
    }

    private static long? ValidateQuantity(long? quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "An item quantity must be positive when provided.");
        }

        return quantity;
    }
}
