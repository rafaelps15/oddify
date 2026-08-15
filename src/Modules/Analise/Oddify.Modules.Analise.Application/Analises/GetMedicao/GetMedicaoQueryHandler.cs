using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Analise.Application.Analises.GetMedicao;

internal sealed class GetMedicaoQueryHandler(IDbConnectionFactory dbConnectionFactory, IFixturesApi fixturesApi)
    : IQueryHandler<GetMedicaoQuery, MedicaoResponse>
{
    public async Task<Result<MedicaoResponse>> Handle(GetMedicaoQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 partida_id AS {nameof(AnaliseParaMedicao.PartidaId)},
                 mercado AS {nameof(AnaliseParaMedicao.Mercado)},
                 prob_poisson_pura AS {nameof(AnaliseParaMedicao.ProbPoissonPura)},
                 prob_dixon_coles AS {nameof(AnaliseParaMedicao.ProbDixonColes)},
                 decisao_do_claude AS {nameof(AnaliseParaMedicao.DecisaoDoClaude)}
             FROM analise.analises_de_partida
             WHERE aprovada_no_filtro = true
             """;

        List<AnaliseParaMedicao> analises = (await connection.QueryAsync<AnaliseParaMedicao>(sql)).AsList();

        IReadOnlyCollection<PartidaResponse> partidas = await fixturesApi.ObterPartidasAsync(
            analises.Select(a => a.PartidaId).Distinct().ToList(), cancellationToken);

        var partidasPorId = partidas
            .Where(p => p.GolsCasa is not null && p.GolsVisitante is not null)
            .ToDictionary(p => p.Id);

        var amostras = analises
            .Where(a => partidasPorId.ContainsKey(a.PartidaId))
            .Select(a =>
            {
                PartidaResponse partida = partidasPorId[a.PartidaId];
                decimal resultadoReal = MercadoResolver.Resolver(a.Mercado, partida.GolsCasa!.Value, partida.GolsVisitante!.Value) ? 1m : 0m;

                return new AmostraDeMedicao(a.ProbPoissonPura, a.ProbDixonColes, resultadoReal, a.DecisaoDoClaude);
            })
            .ToList();

        ResultadoDaMedicao resultado = BrierScoreCalculator.Calcular(amostras);

        return new MedicaoResponse(
            resultado.AmostraTotal,
            resultado.BrierPoissonPuro,
            resultado.BrierDixonColes,
            resultado.AmostraPosClaude,
            resultado.BrierPosClaude);
    }
}
