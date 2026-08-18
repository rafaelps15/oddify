using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.IntegrationEvents;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob (fora do momento em que a mensagem chegou no bus) — ver
// IntegrationEventConsumer<T> (Infrastructure/Inbox), que só grava a mensagem antes de retornar.
public sealed class AnaliseConfirmadaIntegrationEventConsumer(ISender sender)
    : IntegrationEventHandler<AnaliseConfirmadaIntegrationEvent>
{
    public override async Task Handle(AnaliseConfirmadaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new RegistrarAnaliseDisponivelParaApostaCommand(
                integrationEvent.AnaliseId,
                integrationEvent.PartidaId,
                integrationEvent.Mercado,
                integrationEvent.OddDeMercado,
                integrationEvent.ProbabilidadeConfirmada,
                integrationEvent.Reduzida),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(RegistrarAnaliseDisponivelParaApostaCommand), result.Error);
        }
    }
}
