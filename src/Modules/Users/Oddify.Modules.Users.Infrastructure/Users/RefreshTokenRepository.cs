using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.Users;

internal sealed class RefreshTokenRepository(UsersDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public void Insert(RefreshToken refreshToken)
    {
        context.RefreshTokens.Add(refreshToken);
    }
}
