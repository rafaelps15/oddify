using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Application.Users.GetSessions;

internal sealed class GetSessionsQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetSessionsQuery, IReadOnlyCollection<SessionResponse>>
{
    public async Task<Result<IReadOnlyCollection<SessionResponse>>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(SessionResponse.Id)},
                 COALESCE(user_agent, 'Desconhecido') AS {nameof(SessionResponse.UserAgent)},
                 created_at_utc AS {nameof(SessionResponse.CreatedAtUtc)},
                 last_seen_at_utc AS {nameof(SessionResponse.LastSeenAtUtc)}
             FROM users.refresh_tokens
             WHERE user_id = @UserId AND expires_at_utc > @Agora
             ORDER BY last_seen_at_utc DESC
             """;

        List<SessionResponse> sessions = (await connection.QueryAsync<SessionResponse>(
            sql, new { userContext.UserId, Agora = dateTimeProvider.UtcNow })).AsList();

        return sessions;
    }
}
