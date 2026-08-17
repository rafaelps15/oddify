using MassTransit;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Emailing;
using Oddify.Modules.Users.Application.Abstractions.PasswordReset;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

public sealed class SendPasswordResetEmailIntegrationEventConsumer(
    IUserRepository userRepository,
    IEmailSender emailSender,
    IOptions<PasswordResetOptions> options)
    : IConsumer<SendPasswordResetEmailIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SendPasswordResetEmailIntegrationEvent> context)
    {
        SendPasswordResetEmailIntegrationEvent evento = context.Message;

        User? user = await userRepository.GetAsync(evento.UserId, context.CancellationToken);
        if (user is null)
        {
            return;
        }

        string resetLink = $"{options.Value.ResetBaseUrl}?token={Uri.EscapeDataString(evento.RawToken)}";

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Pediram a redefinição da senha desta conta. Se foi você, clique no link abaixo (válido até {evento.ExpiresAtUtc:u} UTC):</p>
             <p><a href="{resetLink}">{resetLink}</a></p>
             <p>Se não foi você, ignore este e-mail — sua senha continua a mesma.</p>
             """;

        await emailSender.SendAsync(user.Email, "Redefinição de senha", body, context.CancellationToken);
    }
}
