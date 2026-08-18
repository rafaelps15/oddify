using Oddify.Common.Application.Emailing;
using Oddify.Common.Application.EventBus;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Presentation.IntegrationEvents;

// Assina o mesmo EmailVerifiedIntegrationEvent que o EmailVerifiedIntegrationEventConsumer de
// Apostas assina (pra criar a banca inicial) — múltiplos handler pro mesmo evento é normal,
// IntegrationEventHandlersFactory despacha pra todos que existirem no assembly Presentation.
// Despachado pelo ProcessInboxJob — ver comentário equivalente em
// AnaliseConfirmadaIntegrationEventConsumer (Apostas).
public sealed class SendWelcomeEmailIntegrationEventConsumer(IUserRepository userRepository, IEmailSender emailSender)
    : IntegrationEventHandler<EmailVerifiedIntegrationEvent>
{
    public override async Task Handle(EmailVerifiedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        User? user = await userRepository.GetAsync(integrationEvent.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        string body =
            $"""
             <p>Olá, {user.FirstName}!</p>
             <p>Seu e-mail foi verificado com sucesso. Sua conta Oddify está pronta pra uso.</p>
             """;

        await emailSender.SendAsync(user.Email, "Bem-vindo à Oddify", body, cancellationToken);
    }
}
