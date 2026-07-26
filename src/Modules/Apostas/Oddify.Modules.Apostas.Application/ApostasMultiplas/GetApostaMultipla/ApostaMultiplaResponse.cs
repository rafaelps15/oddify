using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record ApostaMultiplaResponse(
    Guid Id,
    Guid BancaId,
    decimal OddCombinada,
    decimal Stake,
    ResultadoDaAposta Resultado,
    decimal? LucroOuPerda,
    DateTime CriadaEmUtc);
