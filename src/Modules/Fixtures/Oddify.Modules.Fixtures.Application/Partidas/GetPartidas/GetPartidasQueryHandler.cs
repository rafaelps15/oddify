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
                 gols_visitante AS {nameof(PartidaResponse.GolsVisitante)}
             FROM fixtures.partidas
             WHERE @LigaId IS NULL OR liga_id = @LigaId
             ORDER BY data_utc DESC
             """;

        IReadOnlyCollection<PartidaResponse> result = (await connection.QueryAsync<PartidaResponse>(sql, request)).AsList();

        return Result.Success(result);
    }
}
