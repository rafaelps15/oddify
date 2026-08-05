using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.EmailVerification;

internal sealed class EmailVerificationTokenRepository(UsersDbContext context) : IEmailVerificationTokenRepository
{
    public async Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await context.EmailVerificationTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmailVerificationToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);
    }

    public void Insert(EmailVerificationToken token)
    {
        context.EmailVerificationTokens.Add(token);
    }
}
