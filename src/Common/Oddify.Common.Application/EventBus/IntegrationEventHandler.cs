namespace Oddify.Common.Application.EventBus;

// Mesma ponte de DomainEventHandler<T>, mas pro lado de integration event consumido de outro
// módulo — quem processa a inbox (Common.Infrastructure/Inbox) não conhece o tipo concreto do
// evento em tempo de compilação.
public abstract class IntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    public Task Handle(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default) =>
        Handle((TIntegrationEvent)integrationEvent, cancellationToken);

    public abstract Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
