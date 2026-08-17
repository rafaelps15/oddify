using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

internal sealed class PartidaEncerradaDomainEventHandler(
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IDomainEventHandler<PartidaEncerradaDomainEvent>
{
    public async Task Handle(PartidaEncerradaDomainEvent notification, CancellationToken cancellationToken)
    {
        outboxWriter.Enqueue(new PartidaEncerradaIntegrationEvent(
            Guid.NewGuid(),
            dateTimeProvider.UtcNow,
            notification.PartidaId,
            notification.GolsCasa,
            notification.GolsVisitante));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
