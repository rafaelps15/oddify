using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;

// Despachado pelo OutboxProcessorJob — ver comentário equivalente em
// LigaConfiguradaCriadaDomainEventHandler.
internal sealed class LigaMediasAtualizadasDomainEventHandler(
    ILigaConfiguradaRepository ligaConfiguradaRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<LigaMediasAtualizadasDomainEvent>
{
    public async Task Handle(LigaMediasAtualizadasDomainEvent notification, CancellationToken cancellationToken)
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
