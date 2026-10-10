using System.Globalization;
using System.Text.RegularExpressions;
using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Application.Imports;

public sealed partial class TextHuntSessionParser
{
    private static readonly string[] FieldNames =
    [
        "Raw XP Gain",
        "Raw XP/h",
        "Session length",
        "Killed Monsters",
        "Looted Items",
        "XP Gain",
        "XP/h",
        "Damage/h",
        "Healing/h",
        "Session",
        "Balance",
        "Damage",
        "Healing",
        "Loot",
        "Supplies"
    ];

    private static readonly Regex FieldsRegex = CreateFieldsRegex();

    public HuntParseResult Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var errors = new List<HuntParseError>();
        var fields = ReadFields(content, errors);
        var (start, end) = ReadSessionDates(content, errors);

        var duration = ReadDuration(fields, errors);
        var balance = ReadDecimal(fields, "Balance", errors, allowNegative: true);
        var damage = ReadInteger(fields, "Damage", errors);
        var damagePerHour = ReadDecimal(fields, "Damage/h", errors);
        var healing = ReadInteger(fields, "Healing", errors);
        var healingPerHour = ReadDecimal(fields, "Healing/h", errors);
        var loot = ReadDecimal(fields, "Loot", errors);
        var supplies = ReadDecimal(fields, "Supplies", errors);
        var xpGain = ReadInteger(fields, "XP Gain", errors);
        var xpPerHour = ReadDecimal(fields, "XP/h", errors);
        var rawXpGain = ReadInteger(fields, "Raw XP Gain", errors);
        var rawXpPerHour = ReadDecimal(fields, "Raw XP/h", errors);
        var killedMonsters = ReadRecords(
            fields,
            "Killed Monsters",
            "monster",
            (name, count) => new KilledMonster(name, count),
            errors);
        var lootedItems = ReadRecords(
            fields,
            "Looted Items",
            "item",
            (name, quantity) => new LootedItem(name, quantity),
            errors);

        if (errors.Count > 0)
        {
            return HuntParseResult.Failure(errors);
        }

        var statistics = new HuntStatistics(
            balance,
            damage,
            damagePerHour,
            healing,
            healingPerHour,
            loot,
            supplies,
            xpGain,
            xpPerHour,
            rawXpGain,
            rawXpPerHour);

        return HuntParseResult.Success(new ParsedHuntSession(
            HuntInputFormat.Text,
            content,
            start,
            end,
            duration,
            statistics,
            killedMonsters,
            lootedItems));
    }

    private static Dictionary<string, string> ReadFields(
        string content,
        ICollection<HuntParseError> errors)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in FieldsRegex.Matches(content))
        {
            var name = match.Groups["name"].Value;
            if (!fields.TryAdd(name, match.Groups["value"].Value.Trim()))
            {
                errors.Add(new HuntParseError(name, "The field appears more than once."));
            }
        }

        return fields;
    }

    private static (DateTimeOffset? Start, DateTimeOffset? End) ReadSessionDates(
        string content,
        ICollection<HuntParseError> errors)
    {
        var match = SessionDatesRegex().Match(content);
        if (match.Success)
        {
            if (HuntValueParser.TryParseLocalDateTime(
                match.Groups["startDate"].Value,
                match.Groups["startTime"].Value,
                out var start) &&
                HuntValueParser.TryParseLocalDateTime(
                    match.Groups["endDate"].Value,
                    match.Groups["endTime"].Value,
                    out var end))
            {
                if (end < start)
                {
                    errors.Add(new HuntParseError("Session dates", "Session end cannot be earlier than session start."));
                    return (null, null);
                }

                return (start, end);
            }

            errors.Add(new HuntParseError("Session dates", "The session dates are not valid."));
            return (null, null);
        }

        if (FromKeywordRegex().IsMatch(content))
        {
            errors.Add(new HuntParseError(
                "Session dates",
                "Expected dates in the format \"From yyyy-MM-dd, HH:mm:ss to yyyy-MM-dd, HH:mm:ss\"."));
        }

        return (null, null);
    }

    private static TimeSpan? ReadDuration(
        IReadOnlyDictionary<string, string> fields,
        ICollection<HuntParseError> errors)
    {
        var durationText = GetField(fields, "Session length") ?? GetField(fields, "Session");
        if (durationText is null)
        {
            return null;
        }

        if (HuntValueParser.TryParseDuration(durationText, out var duration))
        {
            return duration;
        }

        errors.Add(new HuntParseError("Session", "The session duration is invalid."));
        return null;
    }

    private static decimal? ReadDecimal(
        IReadOnlyDictionary<string, string> fields,
        string name,
        ICollection<HuntParseError> errors,
        bool allowNegative = false)
    {
        var value = GetField(fields, name);
        if (value is null)
        {
            return null;
        }

        if (HuntValueParser.TryParseDecimal(value, allowNegative, out var number))
        {
            return number;
        }

        errors.Add(new HuntParseError(name, "Expected a valid non-negative number."));
        return null;
    }

    private static long? ReadInteger(
        IReadOnlyDictionary<string, string> fields,
        string name,
        ICollection<HuntParseError> errors)
    {
        var value = GetField(fields, name);
        if (value is null)
        {
            return null;
        }

        if (HuntValueParser.TryParseInteger(value, out var number))
        {
            return number;
        }

        errors.Add(new HuntParseError(name, "Expected a valid non-negative whole number."));
        return null;
    }

    private static IReadOnlyList<T> ReadRecords<T>(
        IReadOnlyDictionary<string, string> fields,
        string name,
        string recordType,
        Func<string, long?, T> createRecord,
        ICollection<HuntParseError> errors)
    {
        var value = GetField(fields, name);
        if (value is null || string.IsNullOrWhiteSpace(value) ||
            string.Equals(value, "None", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var records = new List<T>();
        var lineNumber = 0;
        foreach (var line in value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            lineNumber++;
            var match = RecordLineRegex().Match(line);
            if (!match.Success)
            {
                errors.Add(new HuntParseError(
                    $"{name} line {lineNumber}",
                    $"Expected an optional count followed by a {recordType} name (for example, \"2x {recordType} name\")."));
                continue;
            }

            long? count = null;
            if (match.Groups["count"].Success)
            {
                if (!HuntValueParser.TryParseInteger(match.Groups["count"].Value, out var parsedCount) ||
                    parsedCount == 0)
                {
                    errors.Add(new HuntParseError(
                        $"{name} line {lineNumber}",
                        "The count must be a positive whole number."));
                    continue;
                }

                count = parsedCount;
            }

            try
            {
                records.Add(createRecord(match.Groups["name"].Value, count));
            }
            catch (ArgumentException)
            {
                errors.Add(new HuntParseError($"{name} line {lineNumber}", "The record name is required."));
            }
        }

        return records;
    }

    private static string? GetField(IReadOnlyDictionary<string, string> fields, string name)
    {
        if (name == "Session length" && fields.TryGetValue(name, out var length))
        {
            return length;
        }

        return fields.TryGetValue(name == "Session length" ? "Session" : name, out var value)
            ? value
            : null;
    }

    private static Regex CreateFieldsRegex()
    {
        var alternatives = string.Join(
            "|",
            FieldNames.OrderByDescending(name => name.Length).Select(Regex.Escape));
        return new Regex(
            $@"(?<name>{alternatives}[ \t]*):[ \t]*(?<value>.*?)(?=\s+(?:{alternatives})[ \t]*:|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    }

    [GeneratedRegex(
        @"\bFrom\s+(?<startDate>\d{4}-\d{2}-\d{2}),\s*(?<startTime>\d{2}:\d{2}:\d{2})\s+to\s+(?<endDate>\d{4}-\d{2}-\d{2}),\s*(?<endTime>\d{2}:\d{2}:\d{2})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SessionDatesRegex();

    [GeneratedRegex(@"\bFrom\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FromKeywordRegex();

    [GeneratedRegex(@"^\s*(?:(?<count>\d+)\s*x\s+)?(?<name>.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RecordLineRegex();
}
