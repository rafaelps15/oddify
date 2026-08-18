using Microsoft.Extensions.Options;
using NSubstitute;
using Oddify.Common.Application.Emailing;
using Oddify.Modules.Users.Application.Abstractions.EmailVerification;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;
using Oddify.Modules.Users.Presentation.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Users.IntegrationEvents;

public sealed class SendVerificationEmailIntegrationEventConsumerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IOptions<EmailVerificationOptions> _options = Options.Create(new EmailVerificationOptions());

    private SendVerificationEmailIntegrationEventConsumer CriarConsumer() => new(_userRepository, _emailSender, _options);

    [Fact]
    public async Task Handle_should_send_email_with_the_raw_token_to_the_user()
    {
        var user = User.Create("user@example.com", "hash", "Ada", "Lovelace");
        var evento = new SendVerificationEmailIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, user.Id, "raw-token", DateTime.UtcNow.AddHours(24));
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            "user@example.com",
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("raw-token")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_do_nothing_when_user_not_found()
    {
        var evento = new SendVerificationEmailIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), "raw-token", DateTime.UtcNow.AddHours(24));
        _userRepository.GetAsync(evento.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
