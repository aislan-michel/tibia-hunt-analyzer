namespace TibiaHuntAnalyzer.Application.Sessions;

public sealed record SessionRecord(string Name, long? Count)
{
    public string Display => Count is null ? Name : $"{Count} x {Name}";
}
