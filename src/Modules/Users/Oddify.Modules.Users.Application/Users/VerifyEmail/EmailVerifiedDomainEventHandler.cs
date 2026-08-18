using Microsoft.Extensions.Logging;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.Users;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Application.Users.VerifyEmail;

// Despachado pelo OutboxProcessorJob (fora do request original de VerifyEmail). Só faz o que
// pode ser feito localmente/sem I/O externo: re-consulta o User e loga a auditoria estruturada
// (não existe tabela de auditoria neste repo — Serilog é o mecanismo já estabelecido). O e-mail
// de boas-vindas e a notificação pra Apostas criar a banca inicial exigem entrega garantida, então
// enfileira o integration event via IOutboxWriter em vez de publicar direto — este handler não
// cria nenhuma entidade nova cujo próprio SaveChanges já capturaria o evento, então precisa do
// enqueue explícito. SendWelcomeEmailIntegrationEventConsumer (Users) e
// EmailVerifiedIntegrationEventConsumer (Apostas) reagem a ele depois.
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

        // IOutboxWriter só marca a mensagem no ChangeTracker — precisa deste SaveChanges pra
        // persistir de verdade. OutboxProcessorJob não gerencia EF, só a própria tabela
        // outbox_messages via ADO.NET puro.
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Auditoria: e-mail verificado — UserId={UserId} Email={Email} OccurredOnUtc={OccurredOnUtc}")]
    private static partial void LogEmailVerifiedAudit(ILogger logger, Guid userId, string email, DateTime occurredOnUtc);
}
