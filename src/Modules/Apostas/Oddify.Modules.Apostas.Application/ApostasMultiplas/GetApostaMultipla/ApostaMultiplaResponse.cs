using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record ApostaMultiplaResponse(
    Guid Id,
    Guid BancaId,
    decimal OddCombinada,
    decimal Stake,
    ResultadoDaAposta Resultado,
    decimal? LucroOuPerda,
    DateTime CriadaEmUtc)
{
    // Lista mutável (fora do construtor posicional) porque GetApostaMultiplaQueryHandler e
    // GetApostasMultiplasQueryHandler materializam direto nela via Dapper multi-mapping (splitOn) -
    // mesma técnica do template pra parent+children (EventResponse.TicketTypes/OrderResponse.OrderItems).
    public List<PernaResponse> Pernas { get; } = [];
}
