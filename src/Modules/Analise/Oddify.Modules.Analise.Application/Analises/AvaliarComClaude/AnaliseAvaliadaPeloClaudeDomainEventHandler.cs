using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.IntegrationEvents;

namespace Oddify.Modules.Analise.Application.Analises.AvaliarComClaude;

internal sealed class AnaliseAvaliadaPeloClaudeDomainEventHandler(
    IAnaliseDePartidaRepository analiseRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
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

        outboxWriter.Enqueue(new AnaliseConfirmadaIntegrationEvent(
            Guid.NewGuid(),
            dateTimeProvider.UtcNow,
            analise.Id,
            analise.PartidaId,
            analise.Mercado,
            analise.OddDeMercado,
            analise.ProbDixonColes,
            notification.Decisao == DecisaoDoClaude.Reduz));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
