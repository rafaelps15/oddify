using MassTransit;
using Oddify.Common.Application.Emailing;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

// Assina o mesmo EmailVerifiedIntegrationEvent que o EmailVerifiedIntegrationEventConsumer de
// Apostas assina (pra criar a banca inicial) — múltiplos consumers pro mesmo evento é normal em
// pub/sub, o MassTransit já suporta isso nativamente.
public sealed class SendWelcomeEmailIntegrationEventConsumer(IUserRepository userRepository, IEmailSender emailSender)
    : IConsumer<EmailVerifiedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmailVerifiedIntegrationEvent> context)
    {
        User? user = await userRepository.GetAsync(context.Message.UserId, context.CancellationToken);
        if (user is null)
        {
            return;
        }

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Seu e-mail foi verificado com sucesso. Sua conta Oddify está pronta pra uso.</p>
             """;

        await emailSender.SendAsync(user.Email, "Bem-vindo à Oddify", body, context.CancellationToken);
    }
}
