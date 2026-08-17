namespace Oddify.Modules.Users.Domain.Users;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    // Usado só por ResetPasswordCommandHandler, pra derrubar toda sessão aberta com credenciais
    // antigas — carrega tudo, o handler decide o que remover (mesmo idioma de
    // EmailVerificationTokenIssuer.IssueAsync com GetActiveByUserIdAsync).
    Task<IReadOnlyCollection<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    void Insert(RefreshToken refreshToken);

    void Delete(RefreshToken refreshToken);
}
