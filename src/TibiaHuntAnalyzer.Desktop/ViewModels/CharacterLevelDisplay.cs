using System.Globalization;
using TibiaHuntAnalyzer.Application.Analytics;

namespace TibiaHuntAnalyzer.Desktop.ViewModels;

public sealed record CharacterLevelDisplay(
    string CharacterName,
    string Level,
    string SessionStart)
{
    public static CharacterLevelDisplay Create(CharacterLevelPoint point) =>
        new(
            point.CharacterName,
            point.CharacterLevel.ToString(CultureInfo.CurrentCulture),
            point.SessionStart.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
}
