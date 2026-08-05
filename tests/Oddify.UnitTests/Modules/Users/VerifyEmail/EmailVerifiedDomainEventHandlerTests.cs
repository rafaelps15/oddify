using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.Application.Users.VerifyEmail;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Users.VerifyEmail;

public sealed class EmailVerifiedDomainEventHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    // NullLogger em vez de Substitute.For<ILogger<...>>() — Castle DynamicProxy não consegue
    // gerar proxy pra ILogger<T> quando T (o handler) é internal e ILogger<T> vem de um
    // assembly strong-named (Microsoft.Extensions.Logging.Abstractions); NullLogger é uma
    // implementação real, não um proxy dinâmico, então não esbarra nessa limitação.
    private readonly NullLogger<EmailVerifiedDomainEventHandler> _logger = NullLogger<EmailVerifiedDomainEventHandler>.Instance;

    private EmailVerifiedDomainEventHandler CriarHandler() => new(_userRepository, _outboxWriter, _unitOfWork, _dateTimeProvider, _logger);

    [Fact]
    public async Task Handle_should_enqueue_integration_event_and_persist()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        await CriarHandler().Handle(new EmailVerifiedDomainEvent(user.Id), CancellationToken.None);

        _outboxWriter.Received(1).Enqueue(Arg.Is<EmailVerifiedIntegrationEvent>(e => e.UserId == user.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_do_nothing_when_user_not_found()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await CriarHandler().Handle(new EmailVerifiedDomainEvent(userId), CancellationToken.None);

        _outboxWriter.DidNotReceive().Enqueue(Arg.Any<EmailVerifiedIntegrationEvent>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
