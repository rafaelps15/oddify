namespace Oddify.Common.Application.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;

    void Subscribe<T>(IIntegrationEventHandler<T> handler)
        where T : IIntegrationEvent;

    // Variante não genérica — usada só por <Module>Module.Initialize, que descobre por reflexão
    // (a partir dos IIntegrationEventHandler<T> já existentes no assembly Presentation do próprio
    // módulo) quais tipos de evento assinar, sem o próprio Infrastructure precisar referenciar o
    // projeto IntegrationEvents de outro módulo (exceção reservada só a Presentation, ver
    // CLAUDE.md §2/§10).
    void Subscribe(Type eventType, IIntegrationEventHandler handler);
}
