using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;

public sealed record PerfilDoApostadorResponse(
    Guid BancaId,
    decimal EntradaMedia,
    decimal UnidadeSugerida,
    decimal? DisciplinaDeStake,
    ResultadoDaAposta? SequenciaAtualTipo,
    int SequenciaAtualQuantidade,
    int PiorSequenciaDeReds,
    IReadOnlyCollection<RecomendacaoResponse> Recomendacoes);
