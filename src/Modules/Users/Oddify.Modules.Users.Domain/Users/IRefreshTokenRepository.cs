namespace Oddify.Modules.Users.Domain.Users;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    void Insert(RefreshToken refreshToken);
}
