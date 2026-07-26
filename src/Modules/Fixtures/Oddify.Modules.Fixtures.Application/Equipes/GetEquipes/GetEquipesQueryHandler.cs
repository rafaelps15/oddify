using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipes;

internal sealed class GetEquipesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEquipesQuery, IReadOnlyCollection<EquipeResponse>>
{
    public async Task<Result<IReadOnlyCollection<EquipeResponse>>> Handle(GetEquipesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(EquipeResponse.Id)},
                 id_externo AS {nameof(EquipeResponse.IdExterno)},
                 nome AS {nameof(EquipeResponse.Nome)},
                 liga_id AS {nameof(EquipeResponse.LigaId)}
             FROM fixtures.equipes
             WHERE liga_id = @LigaId
             ORDER BY nome
             """;

        IReadOnlyCollection<EquipeResponse> result = (await connection.QueryAsync<EquipeResponse>(sql, request)).AsList();

        return Result.Success(result);
    }
}
