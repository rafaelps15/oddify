using MassTransit;
using Oddify.Modules.Analise.IntegrationEvents;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

public sealed class AnaliseConfirmadaIntegrationEventConsumer(
    IAnaliseDisponivelParaApostaRepository repository,
    IUnitOfWork unitOfWork)
    : IConsumer<AnaliseConfirmadaIntegrationEvent>
{
    public async Task Consume(ConsumeContext<AnaliseConfirmadaIntegrationEvent> context)
    {
        AnaliseConfirmadaIntegrationEvent evento = context.Message;

        var disponivel = AnaliseDisponivelParaAposta.Create(
            evento.AnaliseId, evento.PartidaId, evento.Mercado, evento.OddDeMercado,
            evento.ProbabilidadeConfirmada, evento.Reduzida);

        repository.Insert(disponivel);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
