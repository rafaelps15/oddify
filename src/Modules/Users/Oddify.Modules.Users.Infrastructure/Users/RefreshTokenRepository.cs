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

    public async Task<RefreshToken?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync(cancellationToken);
    }

    public void Insert(RefreshToken refreshToken)
    {
        context.RefreshTokens.Add(refreshToken);
    }

    public void Delete(RefreshToken refreshToken)
    {
        context.RefreshTokens.Remove(refreshToken);
    }
}
