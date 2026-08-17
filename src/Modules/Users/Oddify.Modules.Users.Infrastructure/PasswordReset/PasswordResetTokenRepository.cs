using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.PasswordReset;

internal sealed class PasswordResetTokenRepository(UsersDbContext context) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await context.PasswordResetTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Insert(PasswordResetToken token)
    {
        context.PasswordResetTokens.Add(token);
    }
}
