using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.Users;

internal sealed class UserRepository(UsersDbContext context) : IUserRepository
{
    public async Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public void Insert(User user)
    {
        context.Users.Add(user);
    }
}
