namespace TibiaHuntAnalyzer.Application.Imports;

public sealed class HuntParseResult
{
    private HuntParseResult(ParsedHuntSession? session, IReadOnlyList<HuntParseError> errors)
    {
        Session = session;
        Errors = errors;
    }

    public ParsedHuntSession? Session { get; }

    public IReadOnlyList<HuntParseError> Errors { get; }

    public bool IsSuccess => Session is not null && Errors.Count == 0;

    public static HuntParseResult Success(ParsedHuntSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new HuntParseResult(session, []);
    }

    public static HuntParseResult Failure(IEnumerable<HuntParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.ToArray();
        if (errorList.Length == 0)
        {
            throw new ArgumentException("At least one parse error is required.", nameof(errors));
        }

        return new HuntParseResult(null, errorList);
    }
}
