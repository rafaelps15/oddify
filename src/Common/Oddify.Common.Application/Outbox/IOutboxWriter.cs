using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Application.Outbox
{
    // Enfileiramento explícito de integration event, pra código que não é ele mesmo despachado pelo
    // OutboxProcessorJob. Grava na mesma tabela outbox_messages que os domain events capturados
    // automaticamente pelo interceptor; OutboxProcessorJob reconhece o tipo real gravado e publica
    // direto no bus em vez de tentar achar um IDomainEventHandler<T> local.
    public interface IOutboxWriter
    {
        void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent;
    }
}
