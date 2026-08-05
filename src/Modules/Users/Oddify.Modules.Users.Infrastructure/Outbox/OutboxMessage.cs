namespace Oddify.Modules.Users.Infrastructure.Outbox;

// Uma linha da outbox: guarda a mensagem serializada (Type + Content) até o
// OutboxProcessorJob publicá-la no bus. RetryCount conta tentativas falhas; FailedAtUtc marca
// quando o limite de tentativas estourou (a mensagem para de ser repescada, mas fica na tabela
// pra inspeção).
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

    public static OutboxMessage Create(string type, string content, DateTime occurredOnUtc)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc,
            RetryCount = 0
        };
    }
}
