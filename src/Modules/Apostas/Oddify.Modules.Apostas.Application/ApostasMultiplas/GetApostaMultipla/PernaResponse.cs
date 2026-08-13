using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record PernaResponse(Guid Id, string Mercado, decimal Odd, Guid PartidaId, ResultadoDaAposta Resultado);
