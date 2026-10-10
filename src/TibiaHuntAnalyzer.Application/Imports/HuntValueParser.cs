using System.Globalization;
using System.Text.RegularExpressions;

namespace TibiaHuntAnalyzer.Application.Imports;

internal static partial class HuntValueParser
{
    private const NumberStyles IntegerStyles = NumberStyles.Integer | NumberStyles.AllowThousands;
    private const NumberStyles DecimalStyles = NumberStyles.Number;

    public static bool TryParseInteger(string value, out long number)
    {
        number = 0;
        return IntegerPattern().IsMatch(value) &&
            long.TryParse(value, IntegerStyles, CultureInfo.InvariantCulture, out number) &&
            number >= 0;
    }

    public static bool TryParseDecimal(string value, bool allowNegative, out decimal number)
    {
        number = 0;
        return DecimalPattern().IsMatch(value) &&
            decimal.TryParse(value, DecimalStyles, CultureInfo.InvariantCulture, out number) &&
            (allowNegative || number >= 0);
    }

    public static bool TryParseUtcDateTime(string value, out DateTimeOffset dateTime) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out dateTime);

    public static bool TryParseLocalDateTime(
        string date,
        string time,
        out DateTimeOffset dateTime)
    {
        if (DateTime.TryParseExact(
            $"{date} {time}",
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var localDateTime))
        {
            localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            dateTime = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime));
            return true;
        }

        dateTime = default;
        return false;
    }

    public static bool TryParseDuration(string value, out TimeSpan duration)
    {
        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out duration))
        {
            return duration >= TimeSpan.Zero;
        }

        var clockMatch = ClockDurationRegex().Match(value);
        if (clockMatch.Success &&
            int.TryParse(clockMatch.Groups["hours"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var hours) &&
            int.TryParse(clockMatch.Groups["minutes"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) &&
            TryParseOptionalUnit(clockMatch.Groups["seconds"].Value, out var seconds))
        {
            try
            {
                duration = new TimeSpan(hours, minutes, seconds);
                return duration >= TimeSpan.Zero;
            }
            catch (ArgumentOutOfRangeException)
            {
                duration = default;
                return false;
            }
        }

        var unitMatch = UnitDurationRegex().Match(value);
        if (unitMatch.Success &&
            (unitMatch.Groups["hours"].Success ||
             unitMatch.Groups["minutes"].Success ||
             unitMatch.Groups["seconds"].Success) &&
            TryParseOptionalUnit(unitMatch.Groups["hours"].Value, out hours) &&
            TryParseOptionalUnit(unitMatch.Groups["minutes"].Value, out minutes) &&
            TryParseOptionalUnit(unitMatch.Groups["seconds"].Value, out seconds))
        {
            try
            {
                duration = new TimeSpan(hours, minutes, seconds);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                duration = default;
                return false;
            }
        }

        duration = default;
        return false;
    }

    private static bool TryParseOptionalUnit(string value, out int amount)
    {
        amount = 0;
        return value.Length == 0 ||
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out amount);
    }

    [GeneratedRegex(@"^(?<hours>\d+):(?<minutes>\d{2})(?::(?<seconds>\d{2}))?h?$", RegexOptions.IgnoreCase)]
    private static partial Regex ClockDurationRegex();

    [GeneratedRegex(@"^(?:(?<hours>\d+)\s*h)?\s*(?:(?<minutes>\d+)\s*min)?\s*(?:(?<seconds>\d+)\s*s)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UnitDurationRegex();

    [GeneratedRegex(@"^(?:\d+|\d{1,3}(?:,\d{3})+)$", RegexOptions.CultureInvariant)]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"^-?(?:\d+|\d{1,3}(?:,\d{3})+)(?:\.\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalPattern();
}
