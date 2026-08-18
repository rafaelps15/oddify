using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorCampeonato;

internal sealed class GetDesempenhoPorCampeonatoQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext,
    IFixturesApi fixturesApi)
    : IQueryHandler<GetDesempenhoPorCampeonatoQuery, IReadOnlyCollection<DesempenhoResponse>>
{
    public async Task<Result<IReadOnlyCollection<DesempenhoResponse>>> Handle(
        GetDesempenhoPorCampeonatoQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new DesempenhoParametros(
            request.BancaId,
            userContext.UserId,
            (int)ResultadoDaAposta.Pendente,
            (int)ResultadoDaAposta.Anulada);

        // MeioGanha/MeioPerdida contam como acerto/erro parcial (mesma regra de
        // GetDesempenhoPorMercado) — Anulada fica de fora, igual Pendente, por não representar uma
        // decisão de verdade.
        const string sql =
            $"""
             WITH pernas_por_aposta AS (
                 SELECT aposta_multipla_id, COUNT(*) AS qtd_pernas, (array_agg(partida_id))[1] AS unica_partida_id
                 FROM apostas.pernas_de_aposta
                 GROUP BY aposta_multipla_id
             )
             SELECT
                 am.resultado AS {nameof(ApostaComPartidaRow.Resultado)},
                 am.lucro_ou_perda AS {nameof(ApostaComPartidaRow.LucroOuPerda)},
                 am.stake AS {nameof(ApostaComPartidaRow.Stake)},
                 ppa.qtd_pernas AS {nameof(ApostaComPartidaRow.QtdPernas)},
                 ppa.unica_partida_id AS {nameof(ApostaComPartidaRow.PartidaId)}
             FROM apostas.apostas_multiplas am
             JOIN pernas_por_aposta ppa ON ppa.aposta_multipla_id = am.id
             WHERE am.banca_id = @BancaId AND am.usuario_id = @UsuarioId
               AND am.resultado NOT IN (@Pendente, @Anulada)
             """;

        List<ApostaComPartidaRow> rows = (await connection.QueryAsync<ApostaComPartidaRow>(sql, parametros)).AsList();

        IReadOnlyCollection<Guid> partidaIds = rows.Where(r => r.QtdPernas == 1).Select(r => r.PartidaId).Distinct().ToList();
        IReadOnlyCollection<PartidaResumoResponse> partidas = await fixturesApi.ObterPartidasResumoAsync(partidaIds, cancellationToken);
        var partidasPorId = partidas.ToDictionary(p => p.Id);

        var resultado = rows
            .Select(r => (Chave: ChaveDeDesempenho.ResolverPorCampeonato(r.QtdPernas, r.PartidaId, partidasPorId), Row: r))
            .GroupBy(e => e.Chave)
            .Select(g => new DesempenhoResponse(
                g.Key,
                g.Count(),
                g.Count(e => e.Row.Resultado is ResultadoDaAposta.Ganha or ResultadoDaAposta.MeioGanha),
                g.Count(e => e.Row.Resultado is ResultadoDaAposta.Perdida or ResultadoDaAposta.MeioPerdida),
                g.Sum(e => e.Row.LucroOuPerda),
                g.Sum(e => e.Row.Stake) > 0 ? g.Sum(e => e.Row.LucroOuPerda) / g.Sum(e => e.Row.Stake) : null))
            .OrderByDescending(d => d.Lucro)
            .ToList();

        return resultado;
    }

    private sealed record DesempenhoParametros(Guid BancaId, Guid UsuarioId, int Pendente, int Anulada);
}
