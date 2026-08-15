using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

internal sealed class GetDesempenhoPorMercadoQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetDesempenhoPorMercadoQuery, IReadOnlyCollection<DesempenhoResponse>>
{
    public async Task<Result<IReadOnlyCollection<DesempenhoResponse>>> Handle(
        GetDesempenhoPorMercadoQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new DesempenhoParametros(request.BancaId, userContext.UserId, ChaveDeDesempenho.Multipla);

        // "Múltipla" agrupa apostas com mais de uma perna - o lucro da aposta é único (não é
        // atribuível a um mercado especifico quando ela combina mercados diferentes), então
        // atribuir o mesmo lucro a cada mercado tocado infla todos os mercados envolvidos. Só
        // apostas de perna única entram nos mercados reais.
        const string sql =
            $"""
             WITH pernas_por_aposta AS (
                 SELECT aposta_multipla_id, COUNT(*) AS qtd_pernas, MIN(mercado) AS unico_mercado
                 FROM apostas.pernas_de_aposta
                 GROUP BY aposta_multipla_id
             ),
             apostas_com_chave AS (
                 SELECT
                     am.resultado,
                     am.lucro_ou_perda,
                     am.stake,
                     CASE WHEN ppa.qtd_pernas > 1 THEN @ChaveMultipla ELSE ppa.unico_mercado END AS chave
                 FROM apostas.apostas_multiplas am
                 JOIN pernas_por_aposta ppa ON ppa.aposta_multipla_id = am.id
                 WHERE am.banca_id = @BancaId AND am.usuario_id = @UsuarioId AND am.resultado IN (1, 2)
             )
             SELECT
                 chave AS {nameof(DesempenhoResponse.Chave)},
                 COUNT(*)::int AS {nameof(DesempenhoResponse.QuantidadeDeApostas)},
                 COUNT(*) FILTER (WHERE resultado = 1)::int AS {nameof(DesempenhoResponse.Ganhas)},
                 COUNT(*) FILTER (WHERE resultado = 2)::int AS {nameof(DesempenhoResponse.Perdidas)},
                 COALESCE(SUM(lucro_ou_perda), 0) AS {nameof(DesempenhoResponse.Lucro)},
                 CASE WHEN SUM(stake) > 0 THEN SUM(lucro_ou_perda) / SUM(stake) ELSE NULL END AS {nameof(DesempenhoResponse.Roi)}
             FROM apostas_com_chave
             GROUP BY chave
             ORDER BY {nameof(DesempenhoResponse.Lucro)} DESC
             """;

        List<DesempenhoResponse> resultado = (await connection.QueryAsync<DesempenhoResponse>(sql, parametros)).AsList();

        return resultado;
    }

    private sealed record DesempenhoParametros(Guid BancaId, Guid UsuarioId, string ChaveMultipla);
}
