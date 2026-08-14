using MassTransit;
using MediatR;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.IntegrationEvents;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

public sealed class AnaliseConfirmadaIntegrationEventConsumer(ISender sender)
    : IConsumer<AnaliseConfirmadaIntegrationEvent>
{
    public async Task Consume(ConsumeContext<AnaliseConfirmadaIntegrationEvent> context)
    {
        AnaliseConfirmadaIntegrationEvent evento = context.Message;

        Result result = await sender.Send(
            new RegistrarAnaliseDisponivelParaApostaCommand(
                evento.AnaliseId,
                evento.PartidaId,
                evento.Mercado,
                evento.OddDeMercado,
                evento.ProbabilidadeConfirmada,
                evento.Reduzida),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(RegistrarAnaliseDisponivelParaApostaCommand), result.Error);
        }
    }
}
