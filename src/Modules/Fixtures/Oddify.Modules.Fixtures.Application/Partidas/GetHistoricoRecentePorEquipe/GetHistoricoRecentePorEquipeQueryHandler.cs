using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetHistoricoRecentePorEquipe;

internal sealed class GetHistoricoRecentePorEquipeQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetHistoricoRecentePorEquipeQuery, HistoricoDeEquipeResponse>
{
    public async Task<Result<HistoricoDeEquipeResponse>> Handle(
        GetHistoricoRecentePorEquipeQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             WITH jogos AS (
                 SELECT
                     CASE WHEN equipe_casa_id = @EquipeId THEN gols_casa ELSE gols_visitante END AS gols_feitos,
                     CASE WHEN equipe_casa_id = @EquipeId THEN gols_visitante ELSE gols_casa END AS gols_sofridos
                 FROM fixtures.partidas
                 WHERE (equipe_casa_id = @EquipeId OR equipe_visitante_id = @EquipeId)
                   AND situacao IN (1, 2)
                 ORDER BY data_utc DESC
                 LIMIT @MinimoDeJogos
             )
             SELECT
                 CAST(COUNT(*) AS integer) AS {nameof(HistoricoDeEquipeResponse.AmostraDeJogos)},
                 COALESCE(AVG(gols_feitos), 0) AS {nameof(HistoricoDeEquipeResponse.MediaGolsFeitos)},
                 COALESCE(AVG(gols_sofridos), 0) AS {nameof(HistoricoDeEquipeResponse.MediaGolsSofridos)}
             FROM jogos
             """;

        HistoricoDeEquipeResponse result = await connection.QuerySingleAsync<HistoricoDeEquipeResponse>(sql, request);

        return Result.Success(result);
    }
}
