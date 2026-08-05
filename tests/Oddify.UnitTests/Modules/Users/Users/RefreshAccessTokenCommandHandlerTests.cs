using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users;
using Oddify.Modules.Users.Application.Users.RefreshAccessToken;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class RefreshAccessTokenCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private RefreshAccessTokenCommandHandler CriarHandler() => new(
        _userRepository, _refreshTokenRepository, _unitOfWork, _tokenProvider, _dateTimeProvider);

    [Fact]
    public async Task Handle_should_rotate_token_and_return_new_tokens_when_refresh_token_is_valid()
    {
        DateTime agora = DateTime.UtcNow;
        var user = User.Create("user@example.com", "hashed-password", "Ada", "Lovelace");
        var refreshToken = RefreshToken.Create(user.Id, "old-refresh-token", agora.AddDays(1));

        _dateTimeProvider.UtcNow.Returns(agora);
        _refreshTokenRepository.GetByTokenAsync("old-refresh-token", Arg.Any<CancellationToken>()).Returns(refreshToken);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tokenProvider.Create(user).Returns("new-access-token");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh-token");

        var command = new RefreshAccessTokenCommand("old-refresh-token");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(new AccessTokensResponse("new-access-token", "new-refresh-token"));
        refreshToken.Token.Should().Be("new-refresh-token");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_invalid_refresh_token_when_token_is_not_found()
    {
        _refreshTokenRepository.GetByTokenAsync("unknown-token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var command = new RefreshAccessTokenCommand("unknown-token");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.InvalidRefreshToken);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_invalid_refresh_token_when_token_is_expired()
    {
        DateTime agora = DateTime.UtcNow;
        var refreshToken = RefreshToken.Create(Guid.NewGuid(), "expired-token", agora.AddDays(-1));

        _dateTimeProvider.UtcNow.Returns(agora);
        _refreshTokenRepository.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(refreshToken);

        var command = new RefreshAccessTokenCommand("expired-token");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.InvalidRefreshToken);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
