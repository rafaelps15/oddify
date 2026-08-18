using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

internal sealed class GetDesempenhoPorMercadoQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetDesempenhoPorMercadoQuery, IReadOnlyCollection<DesempenhoResponse>>
{
    public async Task<Result<IReadOnlyCollection<DesempenhoResponse>>> Handle(
        GetDesempenhoPorMercadoQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new DesempenhoParametros(
            request.BancaId,
            userContext.UserId,
            ChaveDeDesempenho.Multipla,
            (int)ResultadoDaAposta.Pendente,
            (int)ResultadoDaAposta.Anulada,
            (int)ResultadoDaAposta.Ganha,
            (int)ResultadoDaAposta.MeioGanha,
            (int)ResultadoDaAposta.Perdida,
            (int)ResultadoDaAposta.MeioPerdida);

        // "Múltipla" agrupa apostas com mais de uma perna - o lucro da aposta é único (não é
        // atribuível a um mercado especifico quando ela combina mercados diferentes), então
        // atribuir o mesmo lucro a cada mercado tocado infla todos os mercados envolvidos. Só
        // apostas de perna única entram nos mercados reais.
        //
        // MeioGanha/MeioPerdida contam como acerto/erro parcial (mesma regra do "7G/4R" e da taxa
        // de acerto do Dashboard no front) — Anulada fica de fora dos dois lados, igual Pendente,
        // porque não representa uma decisão de verdade.
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
                 WHERE am.banca_id = @BancaId AND am.usuario_id = @UsuarioId
                   AND am.resultado NOT IN (@Pendente, @Anulada)
             )
             SELECT
                 chave AS {nameof(DesempenhoResponse.Chave)},
                 COUNT(*)::int AS {nameof(DesempenhoResponse.QuantidadeDeApostas)},
                 COUNT(*) FILTER (WHERE resultado IN (@Ganha, @MeioGanha))::int AS {nameof(DesempenhoResponse.Ganhas)},
                 COUNT(*) FILTER (WHERE resultado IN (@Perdida, @MeioPerdida))::int AS {nameof(DesempenhoResponse.Perdidas)},
                 COALESCE(SUM(lucro_ou_perda), 0) AS {nameof(DesempenhoResponse.Lucro)},
                 CASE WHEN SUM(stake) > 0 THEN SUM(lucro_ou_perda) / SUM(stake) ELSE NULL END AS {nameof(DesempenhoResponse.Roi)}
             FROM apostas_com_chave
             GROUP BY chave
             ORDER BY {nameof(DesempenhoResponse.Lucro)} DESC
             """;

        List<DesempenhoResponse> resultado = (await connection.QueryAsync<DesempenhoResponse>(sql, parametros)).AsList();

        return resultado;
    }

    private sealed record DesempenhoParametros(
        Guid BancaId,
        Guid UsuarioId,
        string ChaveMultipla,
        int Pendente,
        int Anulada,
        int Ganha,
        int MeioGanha,
        int Perdida,
        int MeioPerdida);
}
