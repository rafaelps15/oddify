using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Users.GetUser;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.GetUserByIdentityId;

internal sealed class GetUserByIdentityIdQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUserByIdentityIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserByIdentityIdQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(UserResponse.Id)},
                 identity_id AS {nameof(UserResponse.IdentityId)},
                 email AS {nameof(UserResponse.Email)},
                 first_name AS {nameof(UserResponse.FirstName)},
                 last_name AS {nameof(UserResponse.LastName)}
             FROM users.users
             WHERE identity_id = @IdentityId
             """;

        UserResponse? result = await connection.QuerySingleOrDefaultAsync<UserResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(request.IdentityId));
        }

        return result;
    }
}
