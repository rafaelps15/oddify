namespace Oddify.Common.Infrastructure.Inbox;

// Uma linha da inbox: uma mensagem recebida do bus, gravada aqui pelo IntegrationEventConsumer<T>
// ANTES de qualquer processamento — dá durabilidade do lado de quem recebe, espelhando a outbox
// do lado de quem publica. Um job por módulo (ProcessInboxJob) lê essas linhas depois e despacha
// pros IIntegrationEventHandler<T> daquele módulo.
public sealed class InboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; }

    public string Content { get; init; }

    public DateTime OccurredOnUtc { get; init; }

    public DateTime? ProcessedOnUtc { get; init; }

    public string? Error { get; init; }

    public int RetryCount { get; init; }

    public DateTime? FailedAtUtc { get; init; }

    public static InboxMessage Create(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        return new InboxMessage
        {
            Id = id,
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc,
            RetryCount = 0
        };
    }
}
