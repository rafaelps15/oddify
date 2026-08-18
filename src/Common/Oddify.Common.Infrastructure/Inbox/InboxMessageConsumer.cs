namespace Oddify.Common.Infrastructure.Inbox;

// Espelho de OutboxMessageConsumer — registra que um IIntegrationEventHandler<T> específico já
// processou uma inbox message específica, pra IdempotentIntegrationEventHandler não reprocessar.
public sealed class InboxMessageConsumer(Guid inboxMessageId, string name)
{
    public Guid InboxMessageId { get; init; } = inboxMessageId;

    public string Name { get; init; } = name;
}
