using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
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

        IEnumerable<AnaliseParaMedicao> analises = await connection.QueryAsync<AnaliseParaMedicao>(sql);
        var analisesList = analises.ToList();

        var partidasPorId = new Dictionary<Guid, PartidaResponse>();

        foreach (Guid partidaId in analisesList.Select(a => a.PartidaId).Distinct())
        {
            Result<PartidaResponse> partidaResult = await fixturesApi.ObterPartidaAsync(partidaId, cancellationToken);

            if (partidaResult.IsFailure)
            {
                continue;
            }

            PartidaResponse partida = partidaResult.Value;

            if (partida.GolsCasa is null || partida.GolsVisitante is null)
            {
                continue;
            }

            partidasPorId[partidaId] = partida;
        }

        decimal somaErroPoissonPuro = 0m;
        decimal somaErroDixonColes = 0m;
        int amostraTotal = 0;

        decimal somaErroPosClaude = 0m;
        int amostraPosClaude = 0;

        foreach (AnaliseParaMedicao analise in analisesList)
        {
            if (!partidasPorId.TryGetValue(analise.PartidaId, out PartidaResponse? partida))
            {
                continue;
            }

            decimal resultadoReal = MercadoResolver.Resolver(analise.Mercado, partida.GolsCasa!.Value, partida.GolsVisitante!.Value)
                ? 1m
                : 0m;

            decimal erroPoissonPuro = analise.ProbPoissonPura - resultadoReal;
            decimal erroDixonColes = analise.ProbDixonColes - resultadoReal;

            somaErroPoissonPuro += erroPoissonPuro * erroPoissonPuro;
            somaErroDixonColes += erroDixonColes * erroDixonColes;
            amostraTotal++;

            if (analise.DecisaoDoClaude == DecisaoDoClaude.Confirma || analise.DecisaoDoClaude == DecisaoDoClaude.Reduz)
            {
                somaErroPosClaude += erroDixonColes * erroDixonColes;
                amostraPosClaude++;
            }
        }

        decimal brierPoissonPuro = amostraTotal == 0 ? 0m : somaErroPoissonPuro / amostraTotal;
        decimal brierDixonColes = amostraTotal == 0 ? 0m : somaErroDixonColes / amostraTotal;
        decimal? brierPosClaude = amostraPosClaude == 0 ? null : somaErroPosClaude / amostraPosClaude;

        return new MedicaoResponse(amostraTotal, brierPoissonPuro, brierDixonColes, amostraPosClaude, brierPosClaude);
    }

    private sealed record AnaliseParaMedicao(Guid PartidaId, string Mercado, decimal ProbPoissonPura, decimal ProbDixonColes, DecisaoDoClaude DecisaoDoClaude);
}
