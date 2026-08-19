namespace Oddify.Common.Application.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
            where T : IIntegrationEvent;

        void Subscribe<T>(IIntegrationEventHandler<T> handler)
            where T : IIntegrationEvent;

        // Variante não genérica — usada só por <Module>Module.Initialize, que descobre por reflexão
        // quais tipos de evento assinar sem Infrastructure precisar referenciar o projeto
        // IntegrationEvents de outro módulo (exceção reservada só a Presentation, CLAUDE.md §2/§10).
        void Subscribe(Type eventType, IIntegrationEventHandler handler);
    }
}
