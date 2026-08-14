using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record PernaResponse(
    Guid Id,
    string Mercado,
    decimal Odd,
    Guid PartidaId,
    ResultadoDaAposta Resultado,
    string? EquipeCasaNome,
    string? EquipeCasaLogo,
    string? EquipeVisitanteNome,
    string? EquipeVisitanteLogo,
    DateTime? PartidaDataUtc);

// Shape só das colunas de apostas.pernas_de_aposta - nome/escudo dos times vem de uma chamada em
// lote a IFixturesApi.ObterPartidasResumoAsync feita à parte (cross-module, não dá pra fazer num
// JOIN SQL), então PernaResponse (com os campos de partida) nunca é materializado direto do Dapper -
// mesma razão do ApostaMultiplaRow em ApostaMultiplaResponse.cs.
internal sealed record PernaRow(Guid Id, string Mercado, decimal Odd, Guid PartidaId, ResultadoDaAposta Resultado)
{
    public PernaResponse ToResponse(PartidaResumoResponse? partida) =>
        new(
            Id,
            Mercado,
            Odd,
            PartidaId,
            Resultado,
            partida?.EquipeCasaNome,
            partida?.EquipeCasaLogo,
            partida?.EquipeVisitanteNome,
            partida?.EquipeVisitanteLogo,
            partida?.DataUtc);
}
