using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.GetEstatisticasDeEquipe;

internal sealed class GetEstatisticasDeEquipeQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEstatisticasDeEquipeQuery, IReadOnlyCollection<EstatisticaEquipeResponse>>
{
    public async Task<Result<IReadOnlyCollection<EstatisticaEquipeResponse>>> Handle(
        GetEstatisticasDeEquipeQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(EstatisticaEquipeResponse.Id)},
                 partida_id AS {nameof(EstatisticaEquipeResponse.PartidaId)},
                 equipe_id AS {nameof(EstatisticaEquipeResponse.EquipeId)},
                 gols AS {nameof(EstatisticaEquipeResponse.Gols)},
                 finalizacoes AS {nameof(EstatisticaEquipeResponse.Finalizacoes)},
                 escanteios AS {nameof(EstatisticaEquipeResponse.Escanteios)},
                 posse AS {nameof(EstatisticaEquipeResponse.Posse)}
             FROM fixtures.estatisticas_de_equipe
             WHERE partida_id = @PartidaId
             """;

        List<EstatisticaEquipeResponse> result = (await connection.QueryAsync<EstatisticaEquipeResponse>(sql, request)).AsList();

        return result;
    }
}
