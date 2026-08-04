using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetRodadasDisponiveis;

internal sealed class GetRodadasDisponiveisQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetRodadasDisponiveisQuery, IReadOnlyCollection<int>>
{
    public async Task<Result<IReadOnlyCollection<int>>> Handle(GetRodadasDisponiveisQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT DISTINCT rodada
            FROM fixtures.partidas
            WHERE (@LigaId IS NULL OR liga_id = @LigaId)
              AND temporada = @Temporada
            ORDER BY rodada
            """;

        IReadOnlyCollection<int> result = (await connection.QueryAsync<int>(sql, request)).AsList();

        return Result.Success(result);
    }
}
