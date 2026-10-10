using System.Globalization;
using System.Text.Json;
using TibiaHuntAnalyzer.Domain;

namespace TibiaHuntAnalyzer.Application.Imports;

public sealed class JsonHuntSessionParser
{
    public HuntParseResult Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            return HuntParseResult.Failure([
                new HuntParseError("$", "The input is not valid JSON.")
            ]);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return HuntParseResult.Failure([
                    new HuntParseError("$", "The JSON root must be an object.")
                ]);
            }

            var fields = new Fields(document.RootElement);
            var start = fields.ReadDateTime("Session start");
            var end = fields.ReadDateTime("Session end");
            var duration = fields.ReadDuration("Session length");
            var balance = fields.ReadDecimal("Balance", allowNegative: true);
            var damage = fields.ReadInteger("Damage");
            var damagePerHour = fields.ReadDecimal("Damage/h");
            var healing = fields.ReadInteger("Healing");
            var healingPerHour = fields.ReadDecimal("Healing/h");
            var loot = fields.ReadDecimal("Loot");
            var supplies = fields.ReadDecimal("Supplies");
            var xpGain = fields.ReadInteger("XP Gain");
            var xpPerHour = fields.ReadDecimal("XP/h");
            var rawXpGain = fields.ReadInteger("Raw XP Gain");
            var rawXpPerHour = fields.ReadDecimal("Raw XP/h");
            var monsters = fields.ReadRecords(
                "Killed Monsters",
                ["Killed", "Count"],
                (name, count) => new KilledMonster(name, count));
            var items = fields.ReadRecords(
                "Looted Items",
                ["Amount", "Quantity", "Count"],
                (name, quantity) => new LootedItem(name, quantity));

            if (fields.Errors.Count > 0)
            {
                return HuntParseResult.Failure(fields.Errors);
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
                HuntInputFormat.Json,
                content,
                start,
                end,
                duration,
                statistics,
                monsters,
                items));
        }
    }

    private sealed class Fields(JsonElement root)
    {
        public List<HuntParseError> Errors { get; } = [];

        public DateTimeOffset? ReadDateTime(string name)
        {
            var value = Find(name);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (TryGetText(value.Value, out var text) &&
                HuntValueParser.TryParseUtcDateTime(text, out var dateTime))
            {
                return dateTime;
            }

            AddInvalid(name, "Expected a date/time string.");
            return null;
        }

        public TimeSpan? ReadDuration(string name)
        {
            var value = Find(name);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (TryGetText(value.Value, out var text) && HuntValueParser.TryParseDuration(text, out var duration))
            {
                return duration;
            }

            AddInvalid(name, "Expected a duration such as \"01:30:00\" or \"1h 30min\".");
            return null;
        }

        public decimal? ReadDecimal(string name, bool allowNegative = false)
        {
            var value = Find(name);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (TryGetText(value.Value, out var text) &&
                HuntValueParser.TryParseDecimal(text, allowNegative, out var number))
            {
                return number;
            }

            AddInvalid(name, "Expected a number or a numeric string.");
            return null;
        }

        public long? ReadInteger(string name)
        {
            var value = Find(name);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (TryGetText(value.Value, out var text) &&
                HuntValueParser.TryParseInteger(text, out var number))
            {
                return number;
            }

            AddInvalid(name, "Expected a whole number or a numeric string.");
            return null;
        }

        public IReadOnlyList<T> ReadRecords<T>(
            string name,
            IReadOnlyList<string> countPropertyNames,
            Func<string, long?, T> createRecord)
        {
            var value = Find(name);
            if (value is null || value.Value.ValueKind == JsonValueKind.Null)
            {
                return [];
            }

            if (value.Value.ValueKind != JsonValueKind.Array)
            {
                AddInvalid(name, "Expected an array of named records.");
                return [];
            }

            var records = new List<T>();
            var index = 0;
            foreach (var record in value.Value.EnumerateArray())
            {
                var field = $"{name}[{index}]";
                index++;

                if (record.ValueKind != JsonValueKind.Object)
                {
                    AddInvalid(field, "Expected an object with a Name field.");
                    continue;
                }

                var recordName = FindRecordProperty(record, "Name");
                if (recordName is null ||
                    !TryGetText(recordName.Value, out var textName) ||
                    string.IsNullOrWhiteSpace(textName))
                {
                    AddInvalid($"{field}.Name", "A non-empty name is required.");
                    continue;
                }

                var countProperty = FindRecordProperty(record, countPropertyNames);
                long? count = null;
                if (countProperty is not null && countProperty.Value.ValueKind != JsonValueKind.Null)
                {
                    if (!TryGetText(countProperty.Value, out var countText) ||
                        !HuntValueParser.TryParseInteger(countText, out var parsedCount))
                    {
                        AddInvalid(field, "The record count must be a whole number.");
                        continue;
                    }

                    count = parsedCount;
                }

                try
                {
                    records.Add(createRecord(textName, count));
                }
                catch (ArgumentOutOfRangeException)
                {
                    AddInvalid(field, "The record count must be positive when provided.");
                }
            }

            return records;
        }

        private JsonElement? Find(string name)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static JsonElement? FindRecordProperty(JsonElement record, string name)
        {
            foreach (var property in record.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return null;
        }

        private static JsonElement? FindRecordProperty(JsonElement record, IReadOnlyList<string> names)
        {
            foreach (var name in names)
            {
                var value = FindRecordProperty(record, name);
                if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }

        private void AddInvalid(string field, string message)
        {
            Errors.Add(new HuntParseError(field, message));
        }

        private static bool TryGetText(JsonElement value, out string text)
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                text = value.GetString() ?? string.Empty;
                return true;
            }

            if (value.ValueKind == JsonValueKind.Number)
            {
                text = value.GetRawText();
                return true;
            }

            text = string.Empty;
            return false;
        }
    }

}
