using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users;
using Oddify.Modules.Users.Application.Users.Login;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenProvider _tokenProvider = Substitute.For<ITokenProvider>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private LoginCommandHandler CriarHandler() => new(
        _userRepository, _refreshTokenRepository, _unitOfWork, _passwordHasher, _tokenProvider, _dateTimeProvider);

    [Fact]
    public async Task Handle_should_return_tokens_when_credentials_are_valid()
    {
        var user = User.Create("user@example.com", "hashed-password", "Ada", "Lovelace");
        user.MarkEmailAsVerified(DateTime.UtcNow);
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password123!", "hashed-password").Returns(true);
        _tokenProvider.Create(user.Id, user.Email).Returns("access-token");
        _tokenProvider.GenerateRefreshToken().Returns("refresh-token");
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var command = new LoginCommand("user@example.com", "Password123!", "Mozilla/5.0");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().Be(new AccessTokensResponse("access-token", "refresh-token"));
        _refreshTokenRepository.Received(1).Insert(Arg.Is<RefreshToken>(rt => rt.UserId == user.Id && rt.Token == "refresh-token"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_email_not_verified_when_account_is_unverified()
    {
        var user = User.Create("user@example.com", "hashed-password", "Ada", "Lovelace");
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("Password123!", "hashed-password").Returns(true);

        var command = new LoginCommand("user@example.com", "Password123!", "Mozilla/5.0");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.EmailNotVerified);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_invalid_credentials_when_email_is_not_found()
    {
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new LoginCommand("user@example.com", "Password123!", "Mozilla/5.0");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.InvalidCredentials);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_with_invalid_credentials_when_password_is_wrong()
    {
        var user = User.Create("user@example.com", "hashed-password", "Ada", "Lovelace");
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-password", "hashed-password").Returns(false);

        var command = new LoginCommand("user@example.com", "wrong-password", "Mozilla/5.0");

        Result<AccessTokensResponse> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.InvalidCredentials);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
