using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.VerifyEmail;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.VerifyEmail;

public sealed class VerifyEmailCommandHandlerTests
{
    private readonly IEmailVerificationTokenRepository _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private VerifyEmailCommandHandler CriarHandler() => new(_tokenRepository, _userRepository, _unitOfWork, _dateTimeProvider);

    private static (EmailVerificationToken Token, User User) CriarTokenEUsuario(DateTime agora, DateTime expiraEm)
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        var token = EmailVerificationToken.Create(user.Id, "raw-token", expiraEm, agora);
        return (token, user);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_not_found()
    {
        _tokenRepository.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((EmailVerificationToken?)null);

        Result resultado = await CriarHandler().Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(EmailVerificationTokenErrors.NotFound);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_already_consumed()
    {
        DateTime agora = DateTime.UtcNow;
        (EmailVerificationToken token, _) = CriarTokenEUsuario(agora, agora.AddHours(24));
        token.Consume(agora);
        _tokenRepository.GetByTokenHashAsync(EmailVerificationToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(EmailVerificationTokenErrors.AlreadyConsumed);
    }

    [Fact]
    public async Task Handle_should_fail_when_token_expired()
    {
        DateTime agora = DateTime.UtcNow;
        (EmailVerificationToken token, _) = CriarTokenEUsuario(agora.AddHours(-25), agora.AddHours(-1));
        _tokenRepository.GetByTokenHashAsync(EmailVerificationToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(EmailVerificationTokenErrors.Expired);
    }

    [Fact]
    public async Task Handle_should_fail_when_account_already_verified()
    {
        DateTime agora = DateTime.UtcNow;
        (EmailVerificationToken token, User user) = CriarTokenEUsuario(agora, agora.AddHours(24));
        user.MarkEmailAsVerified(agora);
        _tokenRepository.GetByTokenHashAsync(EmailVerificationToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.EmailAlreadyVerified);
    }

    [Fact]
    public async Task Handle_should_mark_user_verified_and_consume_token_together_on_success()
    {
        DateTime agora = DateTime.UtcNow;
        (EmailVerificationToken token, User user) = CriarTokenEUsuario(agora, agora.AddHours(24));
        _tokenRepository.GetByTokenHashAsync(EmailVerificationToken.Hash("raw-token"), Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(agora);

        Result resultado = await CriarHandler().Handle(new VerifyEmailCommand("raw-token"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        token.ConsumedAtUtc.Should().Be(agora);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
