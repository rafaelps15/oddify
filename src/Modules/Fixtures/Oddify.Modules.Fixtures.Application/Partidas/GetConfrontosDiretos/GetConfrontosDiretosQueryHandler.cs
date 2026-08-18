using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetConfrontosDiretos;

internal sealed class GetConfrontosDiretosQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetConfrontosDiretosQuery, IReadOnlyCollection<PartidaResponse>>
{
    public async Task<Result<IReadOnlyCollection<PartidaResponse>>> Handle(
        GetConfrontosDiretosQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Só partidas já decididas (Encerrada=1, Liquidada=2) — um confronto ainda agendado não é
        // "histórico". Casa/visitante podem se inverter entre os dois jogos, então o par de equipes
        // é checado nos dois sentidos.
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
             WHERE situacao IN (1, 2)
               AND (
                 (equipe_casa_id = @EquipeAId AND equipe_visitante_id = @EquipeBId)
                 OR (equipe_casa_id = @EquipeBId AND equipe_visitante_id = @EquipeAId)
               )
             ORDER BY data_utc DESC
             LIMIT @Quantidade
             """;

        List<PartidaResponse> result = (await connection.QueryAsync<PartidaResponse>(sql, request)).AsList();

        return result;
    }
}
