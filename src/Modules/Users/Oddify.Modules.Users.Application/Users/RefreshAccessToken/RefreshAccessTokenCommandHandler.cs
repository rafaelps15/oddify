using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Users.RefreshAccessToken;

internal sealed class RefreshAccessTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RefreshAccessTokenCommand, AccessTokensResponse>
{
    public async Task<Result<AccessTokensResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < dateTimeProvider.UtcNow)
        {
            return Result.Failure<AccessTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        User? user = await userRepository.GetAsync(refreshToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AccessTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        string accessToken = tokenProvider.Create(user.Id, user.Email);
        string newRefreshTokenValue = tokenProvider.GenerateRefreshToken();

        DateTime agora = dateTimeProvider.UtcNow;

        refreshToken.Rotate(newRefreshTokenValue, agora.AddDays(RefreshToken.DefaultExpirationDays), agora);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccessTokensResponse(accessToken, newRefreshTokenValue);
    }
}
