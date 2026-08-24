using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.Modules.Analise.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob.
//
// Nenhuma falha aqui propaga como exceção: a única fonte de Result.Failure de
// RegistrarCotacaoCommand hoje é a validação do FluentValidation (o handler nunca retorna falha de
// negócio) — ou seja, ela representa payload malformado vindo da fonte externa de odds (mercado
// desconhecido, odd inválida etc.), esperado/comum, não uma falha de infraestrutura. Lançar uma
// exceção aqui travaria PERMANENTEMENTE o processamento do lote inteiro do inbox (de qualquer
// módulo) — ProcessInboxJob.Execute não tem try/catch por mensagem — e a mensagem malformada nunca
// seria marcada como processada, virando poison message reprocessada para sempre. O mesmo vale para
// AnalisarPartidaCommand (ex.: só uma odd do grupo do mercado chegou até agora). Ambas as falhas já
// ficam registradas no log estruturado via RequestLoggingPipelineBehavior, então não ficam
// silenciosas — só não são tratadas como erro fatal do consumer.
public sealed class CotacaoColetadaIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<CotacaoColetadaIntegrationEvent>
{
    public override async Task Handle(CotacaoColetadaIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result registrarResult = await sender.Send(
            new RegistrarCotacaoCommand(
                integrationEvent.CotacaoId,
                integrationEvent.PartidaId,
                integrationEvent.Mercado,
                integrationEvent.Odd,
                integrationEvent.Casa,
                integrationEvent.OccurredOnUtc),
            cancellationToken);

        if (registrarResult.IsFailure)
        {
            return;
        }

        // Recalcula a análise (upsert) para esta Partida+Mercado sempre que uma cotação nova é
        // espelhada, para que GET /analises/aprovadas reflita a oportunidade sem intervenção manual.
        await sender.Send(new AnalisarPartidaCommand(integrationEvent.PartidaId, integrationEvent.Mercado), cancellationToken);
    }
}
