using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Analise.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob.
public sealed class CotacaoColetadaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<CotacaoColetadaIntegrationEvent>
{
    public override async Task Handle(CotacaoColetadaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RegistrarCotacaoCommand(
                integrationEvent.CotacaoId,
                integrationEvent.PartidaId,
                integrationEvent.Mercado,
                integrationEvent.Odd,
                integrationEvent.Casa,
                integrationEvent.OccurredOnUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(RegistrarCotacaoCommand), result.Error);
        }
    }
}
