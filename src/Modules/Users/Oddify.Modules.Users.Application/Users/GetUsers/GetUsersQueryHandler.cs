using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Users.GetUser;

namespace Oddify.Modules.Users.Application.Users.GetUsers;

internal sealed class GetUsersQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsersQuery, IReadOnlyCollection<UserResponse>>
{
    public async Task<Result<IReadOnlyCollection<UserResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(UserResponse.Id)},
                 email AS {nameof(UserResponse.Email)},
                 first_name AS {nameof(UserResponse.FirstName)},
                 last_name AS {nameof(UserResponse.LastName)}
             FROM users.users
             ORDER BY first_name
             """;

        List<UserResponse> result = (await connection.QueryAsync<UserResponse>(sql)).AsList();

        return result;
    }
}
