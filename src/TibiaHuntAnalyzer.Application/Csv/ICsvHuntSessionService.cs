namespace TibiaHuntAnalyzer.Application.Csv;

public interface ICsvHuntSessionService
{
    Task ExportAsync(TextWriter writer, CancellationToken cancellationToken = default);

    Task<CsvImportResult> ImportAsync(TextReader reader, CancellationToken cancellationToken = default);
}
