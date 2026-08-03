using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Data;

namespace Oddify.Modules.Users.Infrastructure.Authorization;

internal sealed class PermissionService(IDbConnectionFactory dbConnectionFactory) : IPermissionService
{
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
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

        IEnumerable<string> permissions = await connection.QueryAsync<string>(sql, new { UserId = userId });

        return permissions.ToHashSet();
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
