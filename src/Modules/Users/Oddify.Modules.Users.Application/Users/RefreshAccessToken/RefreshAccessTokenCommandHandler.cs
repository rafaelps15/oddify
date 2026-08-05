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
    private const int RefreshTokenExpirationDays = 7;

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

        string accessToken = tokenProvider.Create(user);
        string newRefreshTokenValue = tokenProvider.GenerateRefreshToken();

        refreshToken.Rotate(newRefreshTokenValue, dateTimeProvider.UtcNow.AddDays(RefreshTokenExpirationDays));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AccessTokensResponse(accessToken, newRefreshTokenValue);
    }
}
