using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;

namespace Oddify.Common.Infrastructure.Authorization;

public sealed class PermissionProvider(IDbConnectionFactory dbConnectionFactory)
{
    public async Task<HashSet<string>> GetForUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT p.name
            FROM users.user_roles ur
            JOIN users.role_permissions rp ON rp.role_id = ur.role_id
            JOIN users.permissions p ON p.id = rp.permission_id
            WHERE ur.user_id = @UserId
            """;

        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);

        IEnumerable<string> permissions = await connection.QueryAsync<string>(command);

        return permissions.ToHashSet();
    }
}
