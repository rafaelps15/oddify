using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

public sealed record OutcomeDeOddsDto(string Casa, string Mercado, decimal Odd);

public sealed record EventoDeOddsExternoDto(
    string IdExterno,
    string NomeEquipeCasa,
    string NomeEquipeVisitante,
    DateTime CommenceTimeUtc,
    IReadOnlyCollection<OutcomeDeOddsDto> Outcomes);

public interface ITheOddsApiClient
{
    Task<Result<IReadOnlyCollection<EventoDeOddsExternoDto>>> GetOddsAsync(string sportKey, CancellationToken cancellationToken = default);

    Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default);
}
