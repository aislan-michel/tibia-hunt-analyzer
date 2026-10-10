namespace TibiaHuntAnalyzer.Domain;

public sealed class HuntStatistics
{
    private HuntStatistics()
    {
    }

    public HuntStatistics(
        decimal? balance = null,
        long? damage = null,
        decimal? damagePerHour = null,
        long? healing = null,
        decimal? healingPerHour = null,
        decimal? loot = null,
        decimal? supplies = null,
        long? xpGain = null,
        decimal? xpPerHour = null,
        long? rawXpGain = null,
        decimal? rawXpPerHour = null)
    {
        Balance = balance;
        Damage = ValidateNonNegative(damage, nameof(damage));
        DamagePerHour = ValidateNonNegative(damagePerHour, nameof(damagePerHour));
        Healing = ValidateNonNegative(healing, nameof(healing));
        HealingPerHour = ValidateNonNegative(healingPerHour, nameof(healingPerHour));
        Loot = ValidateNonNegative(loot, nameof(loot));
        Supplies = ValidateNonNegative(supplies, nameof(supplies));
        XpGain = ValidateNonNegative(xpGain, nameof(xpGain));
        XpPerHour = ValidateNonNegative(xpPerHour, nameof(xpPerHour));
        RawXpGain = ValidateNonNegative(rawXpGain, nameof(rawXpGain));
        RawXpPerHour = ValidateNonNegative(rawXpPerHour, nameof(rawXpPerHour));
    }

    public decimal? Balance { get; }

    public long? Damage { get; }

    public decimal? DamagePerHour { get; }

    public long? Healing { get; }

    public decimal? HealingPerHour { get; }

    public decimal? Loot { get; }

    public decimal? Supplies { get; }

    public long? XpGain { get; }

    public decimal? XpPerHour { get; }

    public long? RawXpGain { get; }

    public decimal? RawXpPerHour { get; }

    private static long? ValidateNonNegative(long? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The value cannot be negative.");
        }

        return value;
    }

    private static decimal? ValidateNonNegative(decimal? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "The value cannot be negative.");
        }

        return value;
    }
}
