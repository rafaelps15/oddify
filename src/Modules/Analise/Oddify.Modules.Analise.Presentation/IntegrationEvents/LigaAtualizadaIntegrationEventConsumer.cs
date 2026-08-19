using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Fixtures.UpsertLiga;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Analise.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob.
public sealed class LigaAtualizadaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<LigaAtualizadaIntegrationEvent>
{
    public override async Task Handle(LigaAtualizadaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new UpsertLigaCommand(
                integrationEvent.LigaId,
                integrationEvent.Nome,
                integrationEvent.MediaDeGols,
                integrationEvent.FatorCasa,
                integrationEvent.Calibrada),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(UpsertLigaCommand), result.Error);
        }
    }
}
