using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

// Despachado pelo OutboxProcessorJob — ver comentário equivalente em
// PartidaAgendadaDomainEventHandler sobre reconsultar o estado completo e usar IOutboxWriter
// (entrega garantida) em vez de PublishAsync direto.
internal sealed class LigaConfiguradaCriadaDomainEventHandler(
    ILigaConfiguradaRepository ligaConfiguradaRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<LigaConfiguradaCriadaDomainEvent>
{
    public async Task Handle(LigaConfiguradaCriadaDomainEvent notification, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaConfiguradaRepository.GetAsync(notification.LigaId, cancellationToken);
        if (liga is null)
        {
            return;
        }

        outboxWriter.Enqueue(
            new LigaAtualizadaIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                liga.Id,
                liga.Nome,
                liga.MediaDeGols,
                liga.FatorCasa,
                liga.Calibrada));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
