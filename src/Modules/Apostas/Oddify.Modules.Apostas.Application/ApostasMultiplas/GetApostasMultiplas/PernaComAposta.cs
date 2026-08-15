using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostasMultiplas;

internal sealed record PernaComAposta(
    Guid ApostaMultiplaId,
    Guid Id,
    string Mercado,
    decimal Odd,
    Guid PartidaId,
    ResultadoDaAposta Resultado);
