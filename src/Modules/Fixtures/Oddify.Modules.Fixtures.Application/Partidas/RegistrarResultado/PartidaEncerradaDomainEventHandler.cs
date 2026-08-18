using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

// Despachado pelo OutboxProcessorJob (fora do request original) — ver comentário equivalente em
// AnaliseAvaliadaPeloClaudeDomainEventHandler sobre por que PublishAsync direto é seguro aqui.
internal sealed class PartidaEncerradaDomainEventHandler(IEventBus eventBus) : IDomainEventHandler<PartidaEncerradaDomainEvent>
{
    public async Task Handle(PartidaEncerradaDomainEvent notification, CancellationToken cancellationToken)
    {
        await eventBus.PublishAsync(
            new PartidaEncerradaIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                notification.PartidaId,
                notification.GolsCasa,
                notification.GolsVisitante),
            cancellationToken);
    }
}
