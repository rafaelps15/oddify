using Microsoft.Extensions.Options;
using Oddify.Common.Application.Emailing;
using Oddify.Common.Application.EventBus;
using Oddify.Modules.Users.Application.Abstractions.EmailVerification;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob — ver comentário equivalente em
// AnaliseConfirmadaIntegrationEventConsumer (Apostas).
public sealed class SendVerificationEmailIntegrationEventConsumer(
    IUserRepository userRepository,
    IEmailSender emailSender,
    IOptions<EmailVerificationOptions> options)
    : IntegrationEventHandler<SendVerificationEmailIntegrationEvent>
{
    public override async Task Handle(SendVerificationEmailIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetAsync(integrationEvent.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        string verificationLink = $"{options.Value.VerificationBaseUrl}?token={Uri.EscapeDataString(integrationEvent.RawToken)}";

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Confirme seu e-mail clicando no link abaixo (válido até {integrationEvent.ExpiresAtUtc:u} UTC):</p>
             <p><a href="{verificationLink}">{verificationLink}</a></p>
             """;

        await emailSender.SendAsync(user.Email, "Confirme seu e-mail", body, cancellationToken);
    }
}
