namespace Oddify.Modules.Users.Domain.PasswordReset;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Insert(PasswordResetToken token);
}
