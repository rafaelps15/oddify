using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.RegistrarCotacao;

internal sealed class CotacaoColetadaDomainEventHandler(
    ICotacaoRepository cotacaoRepository,
    IOutboxWriter outboxWriter,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IDomainEventHandler<CotacaoColetadaDomainEvent>
{
    public async Task Handle(CotacaoColetadaDomainEvent notification, CancellationToken cancellationToken)
    {
        Cotacao? cotacao = await cotacaoRepository.GetAsync(notification.CotacaoId, cancellationToken);

        if (cotacao is null)
        {
            return;
        }

        outboxWriter.Enqueue(new CotacaoColetadaIntegrationEvent(
            Guid.NewGuid(),
            dateTimeProvider.UtcNow,
            cotacao.Id,
            cotacao.PartidaId,
            cotacao.Mercado,
            cotacao.Odd));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
