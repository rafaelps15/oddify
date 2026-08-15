using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidasResumo;

internal sealed class GetPartidasResumoQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetPartidasResumoQuery, IReadOnlyCollection<PartidaResumoResponse>>
{
    public async Task<Result<IReadOnlyCollection<PartidaResumoResponse>>> Handle(
        GetPartidasResumoQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 p.id AS {nameof(PartidaResumoResponse.Id)},
                 p.liga_id AS {nameof(PartidaResumoResponse.LigaId)},
                 l.nome AS {nameof(PartidaResumoResponse.LigaNome)},
                 p.data_utc AS {nameof(PartidaResumoResponse.DataUtc)},
                 ec.id AS {nameof(PartidaResumoResponse.EquipeCasaId)},
                 ec.nome AS {nameof(PartidaResumoResponse.EquipeCasaNome)},
                 ec.logo AS {nameof(PartidaResumoResponse.EquipeCasaLogo)},
                 ev.id AS {nameof(PartidaResumoResponse.EquipeVisitanteId)},
                 ev.nome AS {nameof(PartidaResumoResponse.EquipeVisitanteNome)},
                 ev.logo AS {nameof(PartidaResumoResponse.EquipeVisitanteLogo)}
             FROM fixtures.partidas p
             JOIN fixtures.ligas l ON l.id = p.liga_id
             JOIN fixtures.equipes ec ON ec.id = p.equipe_casa_id
             JOIN fixtures.equipes ev ON ev.id = p.equipe_visitante_id
             WHERE p.id = ANY(@Ids)
             """;

        List<PartidaResumoResponse> result = (await connection.QueryAsync<PartidaResumoResponse>(sql, request)).AsList();

        return result;
    }
}
