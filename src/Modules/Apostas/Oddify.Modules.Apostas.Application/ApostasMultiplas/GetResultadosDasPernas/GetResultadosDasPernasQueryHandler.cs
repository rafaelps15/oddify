using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.PublicApi;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;

internal sealed class GetResultadosDasPernasQueryHandler(IFixturesApi fixturesApi, IAnaliseApi analiseApi)
    : IQueryHandler<GetResultadosDasPernasQuery, IReadOnlyDictionary<Guid, bool>>
{
    public async Task<Result<IReadOnlyDictionary<Guid, bool>>> Handle(
        GetResultadosDasPernasQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> partidaIds = request.Pernas.Select(p => p.PartidaId).Distinct().ToList();
        IReadOnlyCollection<PartidaResponse> partidas = await fixturesApi.ObterPartidasAsync(partidaIds, cancellationToken);
        var partidasPorId = partidas.ToDictionary(p => p.Id);

        Error? erro = request.Pernas.Select(p => PernaValidator.Validar(p, partidasPorId)).FirstOrDefault(e => e is not null);
        if (erro is not null)
        {
            return Result.Failure<IReadOnlyDictionary<Guid, bool>>(erro);
        }

        var resultados = request.Pernas.ToDictionary(
            p => p.PernaId,
            p =>
            {
                PartidaResponse partida = partidasPorId[p.PartidaId];
                return analiseApi.ResolverMercado(p.Mercado, partida.GolsCasa!.Value, partida.GolsVisitante!.Value);
            });

        return resultados;
    }
}
