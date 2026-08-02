using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.PublicApi;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;

internal sealed class GetResultadosDasPernasQueryHandler(IFixturesApi fixturesApi, IAnaliseApi analiseApi)
    : IQueryHandler<GetResultadosDasPernasQuery, IReadOnlyDictionary<Guid, bool>>
{
    public async Task<Result<IReadOnlyDictionary<Guid, bool>>> Handle(
        GetResultadosDasPernasQuery request, CancellationToken cancellationToken)
    {
        var resultados = new Dictionary<Guid, bool>();

        foreach (PernaParaResolver perna in request.Pernas)
        {
            PartidaResponse? partida = await fixturesApi.ObterPartidaAsync(perna.PartidaId, cancellationToken);
            if (partida is null)
            {
                return Result.Failure<IReadOnlyDictionary<Guid, bool>>(Error.NotFound(
                    "ApostasMultiplas.PartidaNaoEncontrada",
                    $"A partida {perna.PartidaId} não foi encontrada"));
            }

            if (partida.GolsCasa is null || partida.GolsVisitante is null)
            {
                return Result.Failure<IReadOnlyDictionary<Guid, bool>>(Error.Problem(
                    "ApostasMultiplas.PartidaNaoEncerrada",
                    $"A partida {perna.PartidaId} ainda não foi encerrada"));
            }

            resultados[perna.PernaId] = analiseApi.ResolverMercado(perna.Mercado, partida.GolsCasa.Value, partida.GolsVisitante.Value);
        }

        return resultados;
    }
}
