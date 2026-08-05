using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.Application.Abstractions.Outbox;

// Escrita explícita na outbox — só o que precisa mesmo de entrega garantida (vai virar mensagem
// no bus via OutboxProcessorJob) passa por aqui. Domain events continuam publicados de forma
// síncrona pelo PublishDomainEventsInterceptor padrão; isto é só pro próximo passo que exige
// durabilidade (mandar e-mail, notificar outro módulo).
public interface IOutboxWriter
{
    void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent;
}
