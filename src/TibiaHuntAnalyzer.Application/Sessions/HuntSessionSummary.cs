namespace TibiaHuntAnalyzer.Application.Sessions;

public sealed record HuntSessionSummary(
    Guid Id,
    string CharacterName,
    int CharacterLevel,
    string HuntLocationName,
    DateTimeOffset SessionStart,
    DateTimeOffset SessionEnd)
{
    public string SessionStartDisplay => SessionStart.ToLocalTime().ToString("g");

    public string SessionEndDisplay => SessionEnd.ToLocalTime().ToString("g");
}
