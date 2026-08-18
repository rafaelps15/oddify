using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.EventBus;

// Bus in-memory artesanal, singleton estático de processo — espelha InMemoryEventBus do projeto de
// referência (Modular Monolith with DDD). Substitui o MassTransit: só um dicionário tipo->handlers
// e um Publish síncrono, sem transporte real e sem redelivery. Handlers são assinados uma vez no
// startup (ver <Module>Module.Initialize) e ficam registrados pelo tempo de vida do processo.
internal sealed class InMemoryEventBus
{
    public static InMemoryEventBus Instance { get; } = new();

    private readonly Dictionary<string, List<IIntegrationEventHandler>> _handlers = [];

    private InMemoryEventBus()
    {
    }

    public void Subscribe<T>(IIntegrationEventHandler<T> handler)
        where T : IIntegrationEvent
    {
        Subscribe(typeof(T), handler);
    }

    public void Subscribe(Type eventType, IIntegrationEventHandler handler)
    {
        string key = eventType.FullName!;

        if (_handlers.TryGetValue(key, out List<IIntegrationEventHandler>? handlers))
        {
            handlers.Add(handler);
        }
        else
        {
            _handlers[key] = [handler];
        }
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, Type eventType, CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(eventType.FullName!, out List<IIntegrationEventHandler>? handlers))
        {
            return;
        }

        foreach (IIntegrationEventHandler handler in handlers)
        {
            await handler.Handle(integrationEvent, cancellationToken);
        }
    }
}
