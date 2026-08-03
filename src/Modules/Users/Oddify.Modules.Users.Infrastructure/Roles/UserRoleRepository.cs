using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.Roles;

internal sealed class UserRoleRepository(UsersDbContext context) : IUserRoleRepository
{
    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.UserRoles
            .SingleOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    public async Task<bool> AnyWithRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await context.UserRoles.AnyAsync(ur => ur.RoleId == roleId, cancellationToken);
    }

    public void Insert(UserRole userRole)
    {
        context.UserRoles.Add(userRole);
    }

    public void Remove(UserRole userRole)
    {
        context.UserRoles.Remove(userRole);
    }
}
