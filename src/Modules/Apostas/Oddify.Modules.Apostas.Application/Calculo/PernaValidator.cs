using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.Calculo;

// Decisão pura sobre dados já carregados (nenhum I/O) — não é Factory/Calculator (não deriva um
// valor novo, só valida) nem Policy (Policies do §17 vivem em Domain/<Aggregate>/Policies, e aqui os
// dados vêm de outro módulo via PublicApi, não de um agregado deste próprio módulo).
internal static class PernaValidator
{
    public static Error? Validar(PernaParaResolver perna, IReadOnlyDictionary<Guid, PartidaResponse> partidasPorId)
    {
        if (!partidasPorId.TryGetValue(perna.PartidaId, out PartidaResponse? partida))
        {
            return Error.NotFound(
                "ApostasMultiplas.PartidaNaoEncontrada",
                $"A partida {perna.PartidaId} não foi encontrada");
        }

        if (partida.GolsCasa is null || partida.GolsVisitante is null)
        {
            return Error.Problem(
                "ApostasMultiplas.PartidaNaoEncerrada",
                $"A partida {perna.PartidaId} ainda não foi encerrada");
        }

        return null;
    }
}
