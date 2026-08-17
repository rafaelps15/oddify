using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;

internal sealed class GetPartidasQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetPartidasQuery, IReadOnlyCollection<PartidaResponse>>
{
    public async Task<Result<IReadOnlyCollection<PartidaResponse>>> Handle(GetPartidasQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // @Status chega como o int subjacente do enum (Dapper mapeia automaticamente) — 0 (Todas)
        // não filtra por situação; 1 (Agendadas) só Agendada (0); 2 (Encerradas) agrupa Encerrada
        // (1) e Liquidada (2). Sem filtro de "ao vivo": não existe esse status hoje (ver
        // StatusFiltroDePartida) — quando a integração de dado ao vivo real existir, entra aqui.
        //
        // cardinality(@Ids) trata array vazio igual a NULL: o model binding do Minimal API resolve
        // `[FromQuery] Guid[]? ids` ausente como array vazio, não `null` — só checar `@Ids IS NULL`
        // deixava esse filtro sempre "ativo" com um array vazio, zerando a lista inteira mesmo sem
        // ninguém pedir filtro por id (bug reproduzido em QA manual: /partidas sem nenhum query
        // param voltava [] com a tabela cheia).
        const string sql =
            $"""
             SELECT
                 id AS {nameof(PartidaResponse.Id)},
                 id_externo AS {nameof(PartidaResponse.IdExterno)},
                 liga_id AS {nameof(PartidaResponse.LigaId)},
                 equipe_casa_id AS {nameof(PartidaResponse.EquipeCasaId)},
                 equipe_visitante_id AS {nameof(PartidaResponse.EquipeVisitanteId)},
                 data_utc AS {nameof(PartidaResponse.DataUtc)},
                 situacao AS {nameof(PartidaResponse.Situacao)},
                 gols_casa AS {nameof(PartidaResponse.GolsCasa)},
                 gols_visitante AS {nameof(PartidaResponse.GolsVisitante)},
                 rodada AS {nameof(PartidaResponse.Rodada)},
                 temporada AS {nameof(PartidaResponse.Temporada)}
             FROM fixtures.partidas
             WHERE (@LigaId IS NULL OR liga_id = @LigaId)
               AND (@Rodada IS NULL OR rodada = @Rodada)
               AND (@Temporada IS NULL OR temporada = @Temporada)
               AND (cardinality(@Ids) IS NULL OR cardinality(@Ids) = 0 OR id = ANY(@Ids))
               AND (
                 @Status = 0
                 OR (@Status = 1 AND situacao = 0)
                 OR (@Status = 2 AND situacao IN (1, 2))
               )
             ORDER BY data_utc DESC
             """;

        List<PartidaResponse> result = (await connection.QueryAsync<PartidaResponse>(sql, request)).AsList();

        return result;
    }
}
