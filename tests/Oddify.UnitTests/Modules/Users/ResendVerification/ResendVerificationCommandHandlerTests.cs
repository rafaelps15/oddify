using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Application.Users.ResendVerification;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.ResendVerification;

public sealed class ResendVerificationCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailVerificationTokenRepository _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private ResendVerificationCommandHandler CriarHandler()
    {
        var issuer = new EmailVerificationTokenIssuer(_tokenRepository, _outboxWriter, _dateTimeProvider);
        return new(_userRepository, issuer, _unitOfWork, _userContext);
    }

    [Fact]
    public async Task Handle_should_fail_when_user_not_found()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userRepository.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Result resultado = await CriarHandler().Handle(new ResendVerificationCommand(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.NotFound(userId));
    }

    [Fact]
    public async Task Handle_should_fail_when_already_verified()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        user.MarkEmailAsVerified(DateTime.UtcNow);
        _userContext.UserId.Returns(user.Id);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Result resultado = await CriarHandler().Handle(new ResendVerificationCommand(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.EmailAlreadyVerified);
    }

    [Fact]
    public async Task Handle_should_issue_new_token_and_persist_on_success()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        _userContext.UserId.Returns(user.Id);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<EmailVerificationToken>)[]);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        Result resultado = await CriarHandler().Handle(new ResendVerificationCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _tokenRepository.Received(1).Insert(Arg.Is<EmailVerificationToken>(t => t.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
