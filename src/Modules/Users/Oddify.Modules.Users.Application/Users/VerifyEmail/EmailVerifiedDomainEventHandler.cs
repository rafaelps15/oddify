using Microsoft.Extensions.Logging;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Application.Users.VerifyEmail;

// Roda de forma síncrona, dentro do próprio request de VerifyEmail (PublishDomainEventsInterceptor
// padrão). Só faz o que pode ser feito localmente/sem I/O externo: re-consulta o User e loga a
// auditoria estruturada (não existe tabela de auditoria neste repo — Serilog é o mecanismo já
// estabelecido). O e-mail de boas-vindas e a notificação pra Apostas criar a banca inicial
// exigem entrega garantida, então não são feitos daqui — só enfileira o integration event na
// outbox; SendWelcomeEmailIntegrationEventConsumer (Users) e EmailVerifiedIntegrationEventConsumer
// (Apostas) reagem a ele depois, fora do request.
internal sealed partial class EmailVerifiedDomainEventHandler(
    IUserRepository userRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    ILogger<EmailVerifiedDomainEventHandler> logger)
    : IDomainEventHandler<EmailVerifiedDomainEvent>
{
    public async Task Handle(EmailVerifiedDomainEvent notification, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetAsync(notification.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        LogEmailVerifiedAudit(logger, user.Id, user.Email, notification.OccurredOnUtc);

        outboxWriter.Enqueue(new EmailVerifiedIntegrationEvent(Guid.NewGuid(), dateTimeProvider.UtcNow, user.Id));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Auditoria: e-mail verificado — UserId={UserId} Email={Email} OccurredOnUtc={OccurredOnUtc}")]
    private static partial void LogEmailVerifiedAudit(ILogger logger, Guid userId, string email, DateTime occurredOnUtc);
}
