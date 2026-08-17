using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.ResetPassword;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private ResetPasswordCommandHandler CriarHandler() => new(
        _tokenRepository, _userRepository, _refreshTokenRepository, _unitOfWork, _passwordHasher, _dateTimeProvider);

    private static (PasswordResetToken Token, User User) CriarTokenEUsuario(DateTime agora, DateTime expiraEm)
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        var token = PasswordResetToken.Create(user.Id, "raw-token", expiraEm, agora);
        return (token, user);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_not_found()
    {
        _tokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((PasswordResetToken?)null);

        Result resultado = await CriarHandler().Handle(new ResetPasswordCommand("raw-token", "NovaSenha123!"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PasswordResetTokenErrors.NotFound);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_already_consumed()
    {
        DateTime agora = DateTime.UtcNow;
        (PasswordResetToken token, _) = CriarTokenEUsuario(agora, agora.AddHours(1));
        token.Consume(agora);
        _tokenRepository.GetByTokenHashAsync(PasswordResetToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new ResetPasswordCommand("raw-token", "NovaSenha123!"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PasswordResetTokenErrors.AlreadyConsumed);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_expired()
    {
        DateTime agora = DateTime.UtcNow;
        (PasswordResetToken token, _) = CriarTokenEUsuario(agora.AddHours(-2), agora.AddHours(-1));
        _tokenRepository.GetByTokenHashAsync(PasswordResetToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new ResetPasswordCommand("raw-token", "NovaSenha123!"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PasswordResetTokenErrors.Expired);
    }

    [Fact]
    public async Task Handle_should_update_password_consume_token_and_revoke_sessions_on_success()
    {
        DateTime agora = DateTime.UtcNow;
        (PasswordResetToken token, User user) = CriarTokenEUsuario(agora, agora.AddHours(1));
        var sessao1 = RefreshToken.Create(user.Id, "rt-1", agora.AddDays(7), agora, "Mozilla/5.0");
        var sessao2 = RefreshToken.Create(user.Id, "rt-2", agora.AddDays(7), agora, "Mozilla/5.0");
        _tokenRepository.GetByTokenHashAsync(PasswordResetToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenRepository.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([sessao1, sessao2]);
        _passwordHasher.Hash("NovaSenha123!").Returns("new-hash");
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new ResetPasswordCommand("raw-token", "NovaSenha123!"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        token.ConsumedAtUtc.Should().Be(agora);
        _refreshTokenRepository.Received(1).Delete(sessao1);
        _refreshTokenRepository.Received(1).Delete(sessao2);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
