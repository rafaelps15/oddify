using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.EventBus
{
    internal class EventBus : IEventBus
    {
        public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent
        {
            return InMemoryEventBus.Instance.PublishAsync(integrationEvent, typeof(T), cancellationToken);
        }

        public void Subscribe<T>(IIntegrationEventHandler<T> handler)
            where T : IIntegrationEvent
        {
            InMemoryEventBus.Instance.Subscribe(handler);
        }

        public void Subscribe(Type eventType, IIntegrationEventHandler handler)
        {
            InMemoryEventBus.Instance.Subscribe(eventType, handler);
        }
    }
}
