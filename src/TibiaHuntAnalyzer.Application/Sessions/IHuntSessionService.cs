namespace TibiaHuntAnalyzer.Application.Sessions;

public interface IHuntSessionService
{
    Task<Guid> SaveAsync(
        SaveHuntSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<HuntSessionDetails?> GetDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HuntSessionSummary>> GetSummariesAsync(
        CancellationToken cancellationToken = default);
}
