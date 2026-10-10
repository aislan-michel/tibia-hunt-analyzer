using TibiaHuntAnalyzer.Application.Imports;

namespace TibiaHuntAnalyzer.Application.Sessions;

public sealed record SaveHuntSessionRequest(
    Guid CharacterId,
    Guid HuntLocationId,
    int CharacterLevel,
    ParsedHuntSession ParsedSession);
