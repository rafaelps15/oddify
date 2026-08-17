using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Users.Application.Users.PasswordReset;
using Oddify.Modules.Users.Application.Users.RequestPasswordReset;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private RequestPasswordResetCommandHandler CriarHandler()
    {
        var issuer = new PasswordResetTokenIssuer(_tokenRepository, _outboxWriter, _dateTimeProvider);
        return new(_userRepository, issuer, _unitOfWork);
    }

    [Fact]
    public async Task Handle_should_return_success_without_issuing_token_when_email_not_found()
    {
        // Anti-enumeration: mesma resposta (sucesso) tanto pra e-mail existente quanto
        // inexistente — quem chama nunca deve conseguir distinguir os dois casos.
        _userRepository.GetByEmailAsync("nobody@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Result resultado = await CriarHandler().Handle(new RequestPasswordResetCommand("nobody@example.com"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _tokenRepository.DidNotReceive().Insert(Arg.Any<PasswordResetToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_issue_token_and_save_when_email_exists()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _tokenRepository.GetActiveByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PasswordResetToken>)[]);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        Result resultado = await CriarHandler().Handle(new RequestPasswordResetCommand("user@example.com"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _tokenRepository.Received(1).Insert(Arg.Is<PasswordResetToken>(t => t.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
