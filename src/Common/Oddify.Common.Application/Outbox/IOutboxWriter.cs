using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Application.Outbox;

// Escrita explícita na outbox — só o que precisa mesmo de entrega garantida (vai virar mensagem
// no bus via OutboxProcessorJob, ver Common.Infrastructure/Outbox) passa por aqui. Domain events
// continuam publicados de forma síncrona pelo PublishDomainEventsInterceptor padrão; isto é só
// pro passo que cruza módulo ou exige durabilidade (mandar e-mail, notificar outro módulo).
public interface IOutboxWriter
{
    void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent;
}
