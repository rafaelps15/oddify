using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Analises.GetAnalise;

namespace Oddify.Modules.Analise.Application.Analises.GetAnalisesAprovadas;

internal sealed class GetAnalisesAprovadasQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetAnalisesAprovadasQuery, IReadOnlyCollection<AnaliseResponse>>
{
    public async Task<Result<IReadOnlyCollection<AnaliseResponse>>> Handle(
        GetAnalisesAprovadasQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(AnaliseResponse.Id)},
                 partida_id AS {nameof(AnaliseResponse.PartidaId)},
                 mercado AS {nameof(AnaliseResponse.Mercado)},
                 prob_poisson_pura AS {nameof(AnaliseResponse.ProbPoissonPura)},
                 prob_dixon_coles AS {nameof(AnaliseResponse.ProbDixonColes)},
                 prob_implicita_da_odd AS {nameof(AnaliseResponse.ProbImplicitaDaOdd)},
                 vantagem AS {nameof(AnaliseResponse.Vantagem)},
                 odd_de_mercado AS {nameof(AnaliseResponse.OddDeMercado)},
                 aprovada_no_filtro AS {nameof(AnaliseResponse.AprovadaNoFiltro)},
                 motivo_do_descarte AS {nameof(AnaliseResponse.MotivoDoDescarte)},
                 decisao_do_claude AS {nameof(AnaliseResponse.DecisaoDoClaude)},
                 justificativa_do_claude AS {nameof(AnaliseResponse.JustificativaDoClaude)},
                 versao_do_prompt AS {nameof(AnaliseResponse.VersaoDoPrompt)},
                 criada_em_utc AS {nameof(AnaliseResponse.CriadaEmUtc)}
             FROM analise.analises_de_partida
             WHERE aprovada_no_filtro = true
             ORDER BY criada_em_utc DESC
             """;

        IEnumerable<AnaliseResponse> result = await connection.QueryAsync<AnaliseResponse>(sql);

        return Result.Success<IReadOnlyCollection<AnaliseResponse>>([.. result]);
    }
}
