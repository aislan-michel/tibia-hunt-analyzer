namespace TibiaHuntAnalyzer.Infrastructure.Csv;

internal sealed class CsvSessionRow
{
    public string RecordType { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string CharacterId { get; set; } = string.Empty;

    public string HuntLocationId { get; set; } = string.Empty;

    public string CharacterLevel { get; set; } = string.Empty;

    public string SessionStart { get; set; } = string.Empty;

    public string SessionEnd { get; set; } = string.Empty;

    public string DurationTicks { get; set; } = string.Empty;

    public string InputFormat { get; set; } = string.Empty;

    public string OriginalInput { get; set; } = string.Empty;

    public string Balance { get; set; } = string.Empty;

    public string Damage { get; set; } = string.Empty;

    public string DamagePerHour { get; set; } = string.Empty;

    public string Healing { get; set; } = string.Empty;

    public string HealingPerHour { get; set; } = string.Empty;

    public string Loot { get; set; } = string.Empty;

    public string Supplies { get; set; } = string.Empty;

    public string XpGain { get; set; } = string.Empty;

    public string XpPerHour { get; set; } = string.Empty;

    public string RawXpGain { get; set; } = string.Empty;

    public string RawXpPerHour { get; set; } = string.Empty;

    public string RecordName { get; set; } = string.Empty;

    public string RecordCount { get; set; } = string.Empty;
}
