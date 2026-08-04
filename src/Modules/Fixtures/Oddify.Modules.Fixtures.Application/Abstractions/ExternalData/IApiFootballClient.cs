using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

public sealed record FixtureExternoDto(
    string IdExterno,
    string EquipeCasaIdExterno,
    string NomeEquipeCasa,
    string EquipeVisitanteIdExterno,
    string NomeEquipeVisitante,
    DateTime DataUtc,
    bool Encerrada,
    int? GolsCasa,
    int? GolsVisitante,
    // Extraído de league.round (ex.: "Regular Season - 4") — a api-football não expõe um número
    // de rodada isolado, só esse rótulo textual; quando o formato não termina em número (fases de
    // playoff/grupo com outro rótulo), fica 0 (ver ApiFootballClient.ExtrairRodada).
    int Rodada);

public interface IApiFootballClient
{
    Task<Result<IReadOnlyCollection<FixtureExternoDto>>> GetFixturesAsync(string ligaIdExterno, int temporada, CancellationToken cancellationToken = default);

    Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default);
}
