using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.GetMedicao;

internal sealed class GetMedicaoQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetMedicaoQuery, MedicaoResponse>
{
    public async Task<Result<MedicaoResponse>> Handle(GetMedicaoQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 a.partida_id AS {nameof(AnaliseParaMedicao.PartidaId)},
                 a.mercado AS {nameof(AnaliseParaMedicao.Mercado)},
                 a.prob_poisson_pura AS {nameof(AnaliseParaMedicao.ProbPoissonPura)},
                 a.prob_dixon_coles AS {nameof(AnaliseParaMedicao.ProbDixonColes)},
                 a.decisao_do_claude AS {nameof(AnaliseParaMedicao.DecisaoDoClaude)},
                 p.gols_casa AS {nameof(AnaliseParaMedicao.GolsCasa)},
                 p.gols_visitante AS {nameof(AnaliseParaMedicao.GolsVisitante)}
             FROM analise.analises_de_partida a
             JOIN analise.partidas p ON p.id = a.partida_id
             WHERE a.aprovada_no_filtro = true AND p.gols_casa IS NOT NULL AND p.gols_visitante IS NOT NULL
             """;

        List<AnaliseParaMedicao> analises = (await connection.QueryAsync<AnaliseParaMedicao>(sql)).AsList();

        var amostras = analises
            .Select(a => new AmostraDeMedicao(
                a.ProbPoissonPura,
                a.ProbDixonColes,
                MercadoResolver.Resolver(a.Mercado, a.GolsCasa, a.GolsVisitante) ? 1m : 0m,
                a.DecisaoDoClaude))
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
