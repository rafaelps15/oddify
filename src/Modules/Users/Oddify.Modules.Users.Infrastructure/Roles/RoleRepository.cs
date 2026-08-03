using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.Roles;

internal sealed class RoleRepository(UsersDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await context.Roles.SingleOrDefaultAsync(r => r.Name == name, cancellationToken);
    }
}
