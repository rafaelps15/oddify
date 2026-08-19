using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;

// Despachado pelo OutboxProcessorJob (fora do request original) — reconsulta a Partida completa
// em vez de confiar só no payload do domain event (mesmo critério do §10 passo 2). Entrega
// garantida via IOutboxWriter porque o Analise espelha esse dado pra decidir se aprova uma
// análise, não é aceitável perder esse evento numa falha entre commit e publish.
internal sealed class PartidaAgendadaDomainEventHandler(
    IPartidaRepository partidaRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<PartidaAgendadaDomainEvent>
{
    public async Task Handle(PartidaAgendadaDomainEvent notification, CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(notification.PartidaId, cancellationToken);
        if (partida is null)
        {
            return;
        }

        outboxWriter.Enqueue(
            new PartidaAgendadaIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                partida.Id,
                partida.LigaId,
                partida.EquipeCasaId,
                partida.EquipeVisitanteId,
                partida.DataUtc));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
