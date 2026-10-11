namespace TibiaHuntAnalyzer.Desktop;

public sealed class DatabaseStartupStatus(string? errorMessage = null)
{
    public string? ErrorMessage { get; private set; } = errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void ReportError(string message) => ErrorMessage = message;
}
