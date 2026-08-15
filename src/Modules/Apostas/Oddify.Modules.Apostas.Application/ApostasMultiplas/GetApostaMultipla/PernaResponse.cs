using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record PernaResponse(Guid PernaId, string Mercado, decimal Odd, Guid PartidaId, ResultadoDaAposta Resultado)
{
    // Nome/escudo dos times e data da partida vêm de uma chamada em lote a
    // IFixturesApi.ObterPartidasResumoAsync feita à parte do SELECT principal (cross-module, não dá
    // pra fazer num JOIN SQL) — por isso ficam fora do construtor posicional (o SELECT nunca
    // preenche essas colunas) e são setados via Enriquecer(...) depois que a resposta já foi
    // materializada pelo Dapper.
    public string? EquipeCasaNome { get; set; }

    public string? EquipeCasaLogo { get; set; }

    public string? EquipeVisitanteNome { get; set; }

    public string? EquipeVisitanteLogo { get; set; }

    public DateTime? PartidaDataUtc { get; set; }

    // PernaId (não Id) porque a query faz multi-mapping Dapper (splitOn) com ApostaMultiplaResponse
    // no mesmo SELECT — os dois têm um "Id" próprio, então o split fica ambíguo com o mesmo nome de
    // coluna (mesma solução que o template usa em TicketTypeResponse.TicketTypeId/OrderItemResponse.OrderItemId).

    public void Enriquecer(PartidaResumoResponse? partida)
    {
        EquipeCasaNome = partida?.EquipeCasaNome;
        EquipeCasaLogo = partida?.EquipeCasaLogo;
        EquipeVisitanteNome = partida?.EquipeVisitanteNome;
        EquipeVisitanteLogo = partida?.EquipeVisitanteLogo;
        PartidaDataUtc = partida?.DataUtc;
    }
}
