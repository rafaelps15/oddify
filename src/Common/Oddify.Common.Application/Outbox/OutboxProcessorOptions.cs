namespace Oddify.Common.Application.Outbox;

public sealed class OutboxProcessorOptions
{
    public bool Enabled { get; init; } = true;

    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10);

    // Usado só pelo job de limpeza — mensagens já processadas com mais tempo que isso são
    // apagadas.
    public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromDays(7);

    public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromDays(1);
}
