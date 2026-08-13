using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Equipes;

namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

internal sealed class GetEquipeQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEquipeQuery, EquipeResponse>
{
    public async Task<Result<EquipeResponse>> Handle(GetEquipeQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(EquipeResponse.Id)},
                 id_externo AS {nameof(EquipeResponse.IdExterno)},
                 nome AS {nameof(EquipeResponse.Nome)},
                 liga_id AS {nameof(EquipeResponse.LigaId)},
                 logo AS {nameof(EquipeResponse.Logo)}
             FROM fixtures.equipes
             WHERE id = @EquipeId
             """;

        EquipeResponse? result = await connection.QuerySingleOrDefaultAsync<EquipeResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<EquipeResponse>(EquipeErrors.NotFound(request.EquipeId));
        }

        return result;
    }
}
