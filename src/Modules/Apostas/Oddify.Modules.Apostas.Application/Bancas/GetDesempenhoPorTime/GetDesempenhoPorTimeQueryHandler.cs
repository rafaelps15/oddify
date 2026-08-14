using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorTime;

// Um "time" é um dos dois lados de uma partida - uma aposta de perna única conta pro desempenho
// dos DOIS times daquela partida (o mercado nem sempre é exclusivo de um dos lados, ex.: total de
// gols/escanteios), então cada linha single-leg gera duas entradas de agregação, uma por time.
internal sealed class GetDesempenhoPorTimeQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext,
    IFixturesApi fixturesApi)
    : IQueryHandler<GetDesempenhoPorTimeQuery, IReadOnlyCollection<DesempenhoResponse>>
{
    private const string ChaveMultipla = "Múltipla";
    private const string ChaveDesconhecida = "Outros";

    public async Task<Result<IReadOnlyCollection<DesempenhoResponse>>> Handle(
        GetDesempenhoPorTimeQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new DesempenhoParametros(request.BancaId, userContext.UserId);

        bool bancaExiste = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM apostas.bancas WHERE id = @BancaId AND usuario_id = @UsuarioId)", parametros);

        if (!bancaExiste)
        {
            return Result.Failure<IReadOnlyCollection<DesempenhoResponse>>(BancaErrors.NotFound(request.BancaId));
        }

        const string sql =
            $"""
             WITH pernas_por_aposta AS (
                 SELECT aposta_multipla_id, COUNT(*) AS qtd_pernas, (array_agg(partida_id))[1] AS unica_partida_id
                 FROM apostas.pernas_de_aposta
                 GROUP BY aposta_multipla_id
             )
             SELECT
                 am.resultado AS {nameof(ApostaRow.Resultado)},
                 am.lucro_ou_perda AS {nameof(ApostaRow.LucroOuPerda)},
                 am.stake AS {nameof(ApostaRow.Stake)},
                 ppa.qtd_pernas AS {nameof(ApostaRow.QtdPernas)},
                 ppa.unica_partida_id AS {nameof(ApostaRow.PartidaId)}
             FROM apostas.apostas_multiplas am
             JOIN pernas_por_aposta ppa ON ppa.aposta_multipla_id = am.id
             WHERE am.banca_id = @BancaId AND am.usuario_id = @UsuarioId AND am.resultado IN (1, 2)
             """;

        List<ApostaRow> rows = (await connection.QueryAsync<ApostaRow>(sql, parametros)).AsList();

        IReadOnlyCollection<Guid> partidaIds = rows.Where(r => r.QtdPernas == 1).Select(r => r.PartidaId).Distinct().ToList();
        IReadOnlyCollection<PartidaResumoResponse> partidas = await fixturesApi.ObterPartidasResumoAsync(partidaIds, cancellationToken);
        var partidasPorId = partidas.ToDictionary(p => p.Id);

        IReadOnlyCollection<DesempenhoResponse> resultado = rows
            .SelectMany(r => ResolverChaves(r, partidasPorId).Select(chave => (Chave: chave, Row: r)))
            .GroupBy(e => e.Chave)
            .Select(g => new DesempenhoResponse(
                g.Key,
                g.Count(),
                g.Count(e => e.Row.Resultado == ResultadoDaAposta.Ganha),
                g.Count(e => e.Row.Resultado == ResultadoDaAposta.Perdida),
                g.Sum(e => e.Row.LucroOuPerda),
                g.Sum(e => e.Row.Stake) > 0 ? g.Sum(e => e.Row.LucroOuPerda) / g.Sum(e => e.Row.Stake) : null))
            .OrderByDescending(d => d.Lucro)
            .ToList();

        return Result.Success(resultado);
    }

    private static IReadOnlyCollection<string> ResolverChaves(ApostaRow row, Dictionary<Guid, PartidaResumoResponse> partidasPorId)
    {
        if (row.QtdPernas > 1)
        {
            return [ChaveMultipla];
        }

        if (!partidasPorId.TryGetValue(row.PartidaId, out PartidaResumoResponse? partida))
        {
            return [ChaveDesconhecida];
        }

        return [partida.EquipeCasaNome, partida.EquipeVisitanteNome];
    }

    private sealed record DesempenhoParametros(Guid BancaId, Guid UsuarioId);

    private sealed record ApostaRow(ResultadoDaAposta Resultado, decimal LucroOuPerda, decimal Stake, int QtdPernas, Guid PartidaId);
}
