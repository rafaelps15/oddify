using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Ligas.GetLiga;

namespace Oddify.Modules.Fixtures.Application.Ligas.GetLigas;

internal sealed class GetLigasQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetLigasQuery, IReadOnlyCollection<LigaResponse>>
{
    public async Task<Result<IReadOnlyCollection<LigaResponse>>> Handle(GetLigasQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(LigaResponse.Id)},
                 id_externo AS {nameof(LigaResponse.IdExterno)},
                 nome AS {nameof(LigaResponse.Nome)},
                 media_de_gols AS {nameof(LigaResponse.MediaDeGols)},
                 fator_casa AS {nameof(LigaResponse.FatorCasa)},
                 calibrada AS {nameof(LigaResponse.Calibrada)},
                 bandeira AS {nameof(LigaResponse.Bandeira)}
             FROM fixtures.ligas
             ORDER BY nome
             """;

        IReadOnlyCollection<LigaResponse> result = (await connection.QueryAsync<LigaResponse>(sql)).AsList();

        return Result.Success(result);
    }
}
