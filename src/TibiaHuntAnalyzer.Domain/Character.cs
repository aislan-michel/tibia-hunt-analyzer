namespace TibiaHuntAnalyzer.Domain;

public sealed class Character
{
    public Character(string name)
    {
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A character name is required.", nameof(name));
        }

        return name.Trim();
    }
}
