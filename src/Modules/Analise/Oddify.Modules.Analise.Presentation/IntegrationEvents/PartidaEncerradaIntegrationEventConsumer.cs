using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarResultadoDaPartida;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Analise.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob — segundo consumidor independente deste evento (Apostas também
// assina, pra liquidar apostas múltiplas; §10 permite múltiplos módulos reagindo ao mesmo evento).
public sealed class PartidaEncerradaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<PartidaEncerradaIntegrationEvent>
{
    public override async Task Handle(PartidaEncerradaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RegistrarResultadoDaPartidaCommand(
                integrationEvent.PartidaId,
                integrationEvent.GolsCasa,
                integrationEvent.GolsVisitante),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(RegistrarResultadoDaPartidaCommand), result.Error);
        }
    }
}
