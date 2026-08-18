using Microsoft.Extensions.Options;
using Oddify.Common.Application.Emailing;
using Oddify.Common.Application.EventBus;
using Oddify.Modules.Users.Application.Abstractions.PasswordReset;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob — ver comentário equivalente em
// AnaliseConfirmadaIntegrationEventConsumer (Apostas).
public sealed class SendPasswordResetEmailIntegrationEventConsumer(
    IUserRepository userRepository,
    IEmailSender emailSender,
    IOptions<PasswordResetOptions> options)
    : IntegrationEventHandler<SendPasswordResetEmailIntegrationEvent>
{
    public override async Task Handle(SendPasswordResetEmailIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetAsync(integrationEvent.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        string resetLink = $"{options.Value.ResetBaseUrl}?token={Uri.EscapeDataString(integrationEvent.RawToken)}";

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Pediram a redefinição da senha desta conta. Se foi você, clique no link abaixo (válido até {integrationEvent.ExpiresAtUtc:u} UTC):</p>
             <p><a href="{resetLink}">{resetLink}</a></p>
             <p>Se não foi você, ignore este e-mail — sua senha continua a mesma.</p>
             """;

        await emailSender.SendAsync(user.Email, "Redefinição de senha", body, cancellationToken);
    }
}
