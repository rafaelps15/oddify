using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Messaging;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.IntegrationEvents;

namespace Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;

// Despachado pelo OutboxProcessorJob (fora do request original) — a durabilidade já vem da
// própria outbox onde este domain event foi capturado, então PublishAsync aqui é seguro mesmo
// sendo um publish direto no bus, sem outbox própria: se o processo morrer antes deste ponto, a
// linha da outbox continua pendente e o job tenta de novo na próxima execução.
internal sealed class AnaliseAvaliadaPeloClaudeDomainEventHandler(
    IAnaliseDePartidaRepository analiseRepository,
    IEventBus eventBus)
    : IDomainEventHandler<AnaliseAvaliadaPeloClaudeDomainEvent>
{
    public async Task Handle(AnaliseAvaliadaPeloClaudeDomainEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Decisao != DecisaoDoClaude.Confirma && notification.Decisao != DecisaoDoClaude.Reduz)
        {
            return;
        }

        AnaliseDePartida? analise = await analiseRepository.GetAsync(notification.AnaliseId, cancellationToken);

        if (analise is null)
        {
            return;
        }

        await eventBus.PublishAsync(
            new AnaliseConfirmadaIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                analise.Id,
                analise.PartidaId,
                analise.Mercado,
                analise.OddDeMercado,
                analise.ProbDixonColes,
                notification.Decisao == DecisaoDoClaude.Reduz),
            cancellationToken);
    }
}
