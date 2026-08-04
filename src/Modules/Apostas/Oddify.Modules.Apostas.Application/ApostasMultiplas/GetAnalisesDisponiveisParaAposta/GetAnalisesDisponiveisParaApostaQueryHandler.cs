using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetAnalisesDisponiveisParaAposta;

internal sealed class GetAnalisesDisponiveisParaApostaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetAnalisesDisponiveisParaApostaQuery, IReadOnlyCollection<AnaliseDisponivelParaApostaResponse>>
{
    public async Task<Result<IReadOnlyCollection<AnaliseDisponivelParaApostaResponse>>> Handle(
        GetAnalisesDisponiveisParaApostaQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Fonte única de "pode virar perna de múltipla": esta tabela (sincronizada via
        // AnaliseConfirmadaIntegrationEventConsumer a partir do módulo Analise) já exclui o que
        // foi vetado/não avaliado (só chega aqui quando o evento é publicado) e o que já foi
        // usado numa múltipla anterior (ja_utilizada) — o front antes recriava só a primeira
        // metade dessa regra filtrando /analises/aprovadas por decisão, sem saber de
        // ja_utilizada nem de "reduzida" (que MontarMultiplaCommandHandler usa pra cortar a
        // stake sugerida pela metade).
        const string sql =
            $"""
             SELECT
                 id AS {nameof(AnaliseDisponivelParaApostaResponse.Id)},
                 partida_id AS {nameof(AnaliseDisponivelParaApostaResponse.PartidaId)},
                 mercado AS {nameof(AnaliseDisponivelParaApostaResponse.Mercado)},
                 odd_de_mercado AS {nameof(AnaliseDisponivelParaApostaResponse.OddDeMercado)},
                 probabilidade_confirmada AS {nameof(AnaliseDisponivelParaApostaResponse.ProbabilidadeConfirmada)},
                 probabilidade_confirmada - (1.0 / odd_de_mercado) AS {nameof(AnaliseDisponivelParaApostaResponse.Vantagem)},
                 reduzida AS {nameof(AnaliseDisponivelParaApostaResponse.Reduzida)}
             FROM apostas.analises_disponiveis_para_aposta
             WHERE ja_utilizada = false
             """;

        IEnumerable<AnaliseDisponivelParaApostaResponse> result =
            await connection.QueryAsync<AnaliseDisponivelParaApostaResponse>(sql);

        return Result.Success<IReadOnlyCollection<AnaliseDisponivelParaApostaResponse>>([.. result]);
    }
}
