using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetRodadaMaisRecenteEncerrada;

internal sealed class GetRodadaMaisRecenteEncerradaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetRodadaMaisRecenteEncerradaQuery, int?>
{
    public async Task<Result<int?>> Handle(GetRodadaMaisRecenteEncerradaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Uma rodada só conta como "completa e encerrada" quando NENHUM jogo dela está fora de
        // Encerrada(1)/Liquidada(2) — daí o FILTER contando o que sobra fora desse conjunto e
        // exigindo zero.
        const string sql =
            """
            SELECT rodada
            FROM fixtures.partidas
            WHERE (@LigaId IS NULL OR liga_id = @LigaId)
              AND temporada = @Temporada
            GROUP BY rodada
            HAVING COUNT(*) FILTER (WHERE situacao NOT IN (1, 2)) = 0
            ORDER BY rodada DESC
            LIMIT 1
            """;

        int? result = await connection.QuerySingleOrDefaultAsync<int?>(sql, request);

        return result;
    }
}
