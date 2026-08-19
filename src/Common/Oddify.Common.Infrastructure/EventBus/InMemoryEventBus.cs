using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.EventBus
{
    internal sealed class InMemoryEventBus
    {
        public static InMemoryEventBus Instance { get; } = new();

        private readonly Dictionary<string, List<IIntegrationEventHandler>> _handlers = new();

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
                _handlers[key] = new List<IIntegrationEventHandler> { handler };
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
}
