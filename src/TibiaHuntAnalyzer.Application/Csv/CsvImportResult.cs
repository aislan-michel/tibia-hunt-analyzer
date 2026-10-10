namespace TibiaHuntAnalyzer.Application.Csv;

public sealed class CsvImportResult
{
    private CsvImportResult(int importedSessionCount, IReadOnlyList<CsvImportError> errors)
    {
        ImportedSessionCount = importedSessionCount;
        Errors = errors;
    }

    public int ImportedSessionCount { get; }

    public IReadOnlyList<CsvImportError> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public static CsvImportResult Success(int importedSessionCount) =>
        new(importedSessionCount, []);

    public static CsvImportResult Failure(IEnumerable<CsvImportError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var errorList = errors.ToArray();
        if (errorList.Length == 0)
        {
            throw new ArgumentException("At least one CSV import error is required.", nameof(errors));
        }

        return new CsvImportResult(0, errorList);
    }
}
