using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

// Despachado pelo OutboxProcessorJob (fora do request original) — ver comentário equivalente em
// AnaliseAvaliadaPeloClaudeDomainEventHandler sobre por que PublishAsync direto é seguro aqui.
internal sealed class CotacaoColetadaDomainEventHandler(ICotacaoRepository cotacaoRepository, IEventBus eventBus)
    : IDomainEventHandler<CotacaoColetadaDomainEvent>
{
    public async Task Handle(CotacaoColetadaDomainEvent notification, CancellationToken cancellationToken)
    {
        Cotacao? cotacao = await cotacaoRepository.GetAsync(notification.CotacaoId, cancellationToken);

        if (cotacao is null)
        {
            return;
        }

        await eventBus.PublishAsync(
            new CotacaoColetadaIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                cotacao.Id,
                cotacao.PartidaId,
                cotacao.Mercado,
                cotacao.Odd,
                cotacao.Casa),
            cancellationToken);
    }
}
