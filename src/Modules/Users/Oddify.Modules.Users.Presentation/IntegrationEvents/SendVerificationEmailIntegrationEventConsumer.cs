using MassTransit;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Emailing;
using Oddify.Modules.Users.Application.Abstractions.EmailVerification;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

public sealed class SendVerificationEmailIntegrationEventConsumer(
    IUserRepository userRepository,
    IEmailSender emailSender,
    IOptions<EmailVerificationOptions> options)
    : IConsumer<SendVerificationEmailIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SendVerificationEmailIntegrationEvent> context)
    {
        SendVerificationEmailIntegrationEvent evento = context.Message;

        User? user = await userRepository.GetAsync(evento.UserId, context.CancellationToken);
        if (user is null)
        {
            return;
        }

        string verificationLink = $"{options.Value.VerificationBaseUrl}?token={Uri.EscapeDataString(evento.RawToken)}";

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Confirme seu e-mail clicando no link abaixo (válido até {evento.ExpiresAtUtc:u} UTC):</p>
             <p><a href="{verificationLink}">{verificationLink}</a></p>
             """;

        await emailSender.SendAsync(user.Email, "Confirme seu e-mail", body, context.CancellationToken);
    }
}
