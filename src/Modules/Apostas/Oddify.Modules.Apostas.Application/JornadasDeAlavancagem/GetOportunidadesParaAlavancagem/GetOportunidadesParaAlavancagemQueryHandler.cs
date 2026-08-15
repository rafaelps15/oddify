using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetOportunidadesParaAlavancagem;

internal sealed class GetOportunidadesParaAlavancagemQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetOportunidadesParaAlavancagemQuery, IReadOnlyCollection<OportunidadeParaAlavancagemResponse>>
{
    public async Task<Result<IReadOnlyCollection<OportunidadeParaAlavancagemResponse>>> Handle(
        GetOportunidadesParaAlavancagemQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Mesma tabela-espelho local que GetAnalisesDisponiveisParaApostaQueryHandler usa (sem
        // precisar cruzar pro módulo Analise) — aqui com o filtro próprio do módulo de Alavancagem:
        // odd <= 1.50 e edge > 0 sempre. DISTINCT ON (partida_id) fica com a melhor candidata (maior
        // probabilidade) de cada jogo distinto; a subconsulta reordena por probabilidade pra pegar
        // as N melhores no geral, entre jogos diferentes.
        const string sql =
            $"""
             SELECT * FROM (
                 SELECT DISTINCT ON (partida_id)
                     id AS {nameof(OportunidadeParaAlavancagemResponse.Id)},
                     partida_id AS {nameof(OportunidadeParaAlavancagemResponse.PartidaId)},
                     mercado AS {nameof(OportunidadeParaAlavancagemResponse.Mercado)},
                     odd_de_mercado AS {nameof(OportunidadeParaAlavancagemResponse.OddDeMercado)},
                     probabilidade_confirmada AS {nameof(OportunidadeParaAlavancagemResponse.ProbabilidadeConfirmada)},
                     probabilidade_confirmada - (1.0 / odd_de_mercado) AS {nameof(OportunidadeParaAlavancagemResponse.Vantagem)}
                 FROM apostas.analises_disponiveis_para_aposta
                 WHERE
                     ja_utilizada = false AND
                     odd_de_mercado <= @OddMaxima AND
                     probabilidade_confirmada - (1.0 / odd_de_mercado) > 0
                 ORDER BY partida_id, probabilidade_confirmada DESC
             ) AS melhores_por_partida
             ORDER BY {nameof(OportunidadeParaAlavancagemResponse.ProbabilidadeConfirmada)} DESC
             LIMIT @Quantidade
             """;

        List<OportunidadeParaAlavancagemResponse> oportunidades = (await connection.QueryAsync<OportunidadeParaAlavancagemResponse>(
            sql, new { RegrasDeAlavancagem.OddMaxima, request.Quantidade })).AsList();

        return oportunidades;
    }
}
