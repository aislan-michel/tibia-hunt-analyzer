namespace TibiaHuntAnalyzer.Application.Csv;

public sealed record CsvImportError(int Row, string Field, string Message);
