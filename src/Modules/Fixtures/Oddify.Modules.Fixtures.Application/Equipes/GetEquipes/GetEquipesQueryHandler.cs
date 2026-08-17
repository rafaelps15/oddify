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

        // cardinality(@Ids) trata array vazio igual a NULL — ver comentário equivalente em
        // GetPartidasQueryHandler (mesmo bug de model binding do Minimal API para `Guid[]?`).
        const string sql =
            $"""
             SELECT
                 id AS {nameof(EquipeResponse.Id)},
                 id_externo AS {nameof(EquipeResponse.IdExterno)},
                 nome AS {nameof(EquipeResponse.Nome)},
                 liga_id AS {nameof(EquipeResponse.LigaId)},
                 logo AS {nameof(EquipeResponse.Logo)}
             FROM fixtures.equipes
             WHERE (@LigaId IS NULL OR liga_id = @LigaId)
               AND (cardinality(@Ids) IS NULL OR cardinality(@Ids) = 0 OR id = ANY(@Ids))
             ORDER BY nome
             """;

        List<EquipeResponse> result = (await connection.QueryAsync<EquipeResponse>(sql, request)).AsList();

        return result;
    }
}
