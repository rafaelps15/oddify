namespace Oddify.Common.Infrastructure.Outbox;

// Uma linha da outbox: guarda o evento serializado (Type + Content) até o OutboxProcessorJob
// despachá-lo. Id é o mesmo Id do evento (não um Guid novo). Sem RetryCount/Error/FailedAtUtc —
// espelha o formato do projeto de referência (Modular Monolith with DDD): uma falha de
// processamento deixa a linha pendente pra ser repescada na próxima rodada do job, sem contagem de
// tentativas nem exaustão.
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public string Type { get; init; }

    public string Content { get; init; }

    public DateTime OccurredOnUtc { get; init; }

    public DateTime? ProcessedOnUtc { get; init; }

    public static OutboxMessage Create(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        return new OutboxMessage
        {
            Id = id,
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc
        };
    }
}
