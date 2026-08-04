using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

internal sealed class GetPartidaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetPartidaQuery, PartidaResponse>
{
    public async Task<Result<PartidaResponse>> Handle(GetPartidaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

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
             WHERE id = @PartidaId
             """;

        PartidaResponse? result = await connection.QuerySingleOrDefaultAsync<PartidaResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<PartidaResponse>(PartidaErrors.NotFound(request.PartidaId));
        }

        return result;
    }
}
