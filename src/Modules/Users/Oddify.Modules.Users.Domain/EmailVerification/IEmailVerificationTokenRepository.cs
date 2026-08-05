namespace Oddify.Modules.Users.Domain.EmailVerification;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Tokens ainda não consumidos (expirado ou não) — usado pra invalidar tudo que ficou "órfão"
    // de uma tentativa anterior antes de emitir um token novo (ver EmailVerificationTokenIssuer).
    Task<IReadOnlyCollection<EmailVerificationToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Insert(EmailVerificationToken token);
}
