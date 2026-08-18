using NSubstitute;
using Oddify.Common.Application.Emailing;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;
using Oddify.Modules.Users.Presentation.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Users.IntegrationEvents;

public sealed class SendWelcomeEmailIntegrationEventConsumerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private SendWelcomeEmailIntegrationEventConsumer CriarConsumer() => new(_userRepository, _emailSender);

    [Fact]
    public async Task Handle_should_send_welcome_email_to_the_user()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, user.Id);
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _emailSender.Received(1).SendAsync("user@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_do_nothing_when_user_not_found()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _userRepository.GetAsync(evento.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
