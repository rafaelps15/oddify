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
    int? GolsVisitante);

public interface IApiFootballClient
{
    Task<Result<IReadOnlyCollection<FixtureExternoDto>>> GetFixturesAsync(string ligaIdExterno, int temporada, CancellationToken cancellationToken = default);

    Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default);
}
