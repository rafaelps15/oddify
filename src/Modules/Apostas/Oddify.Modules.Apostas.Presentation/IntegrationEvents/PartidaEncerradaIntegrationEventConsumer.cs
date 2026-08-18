using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob — ver comentário equivalente em
// AnaliseConfirmadaIntegrationEventConsumer. LiquidarApostasDaPartidaEncerradaCommandHandler é
// melhor esforço (não propaga falha por múltipla ainda incompleta), então um Result.Failure aqui
// só acontece por algo realmente inesperado.
public sealed class PartidaEncerradaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<PartidaEncerradaIntegrationEvent>
{
    public override async Task Handle(PartidaEncerradaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new LiquidarApostasDaPartidaEncerradaCommand(integrationEvent.PartidaId),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(LiquidarApostasDaPartidaEncerradaCommand), result.Error);
        }
    }
}
