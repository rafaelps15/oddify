namespace Oddify.Common.Infrastructure.Outbox;

// Uma linha da outbox: guarda o domain event serializado (Type + Content) até o
// OutboxProcessorJob despachá-lo pros IDomainEventHandler<T> locais daquele módulo. Id é o mesmo
// Id do domain event (não um Guid novo) — é o que IdempotentDomainEventHandler usa como chave em
// outbox_message_consumers. RetryCount conta tentativas falhas; FailedAtUtc marca quando o limite
// de tentativas estourou (a mensagem para de ser repescada, mas fica na tabela pra inspeção).
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; }

    public string Content { get; init; }

    public DateTime OccurredOnUtc { get; init; }

    public DateTime? ProcessedOnUtc { get; init; }

    public string? Error { get; init; }

    public int RetryCount { get; init; }

    public DateTime? FailedAtUtc { get; init; }

    public static OutboxMessage Create(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        return new OutboxMessage
        {
            Id = id,
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc,
            RetryCount = 0
        };
    }
}
