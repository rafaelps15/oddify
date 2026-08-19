using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarPartida;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Analise.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob.
public sealed class PartidaAgendadaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<PartidaAgendadaIntegrationEvent>
{
    public override async Task Handle(PartidaAgendadaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RegistrarPartidaCommand(
                integrationEvent.PartidaId,
                integrationEvent.LigaId,
                integrationEvent.EquipeCasaId,
                integrationEvent.EquipeVisitanteId,
                integrationEvent.DataUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(RegistrarPartidaCommand), result.Error);
        }
    }
}
