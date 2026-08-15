using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Application.Roles.GetUserRoles;

internal sealed class GetUserRolesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUserRolesQuery, IReadOnlyCollection<RoleResponse>>
{
    public async Task<Result<IReadOnlyCollection<RoleResponse>>> Handle(
        GetUserRolesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 r.id AS {nameof(RoleResponse.Id)},
                 r.name AS {nameof(RoleResponse.Name)}
             FROM users.user_roles ur
             JOIN users.roles r ON r.id = ur.role_id
             WHERE ur.user_id = @UserId
             """;

        List<RoleResponse> result = (await connection.QueryAsync<RoleResponse>(sql, request)).AsList();

        return result;
    }
}
