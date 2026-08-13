using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.Modules.Fixtures.Application.Ligas.GetLiga;

internal sealed class GetLigaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetLigaQuery, LigaResponse>
{
    public async Task<Result<LigaResponse>> Handle(GetLigaQuery request, CancellationToken cancellationToken)
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
             WHERE id = @LigaId
             """;

        LigaResponse? liga = await connection.QuerySingleOrDefaultAsync<LigaResponse>(sql, request);

        if (liga is null)
        {
            return Result.Failure<LigaResponse>(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        return liga;
    }
}
