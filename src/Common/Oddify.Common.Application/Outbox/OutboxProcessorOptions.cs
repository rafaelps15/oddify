namespace Oddify.Common.Application.Outbox
{
    public class OutboxProcessorOptions
    {
        public bool Enabled { get; init; } = true;

        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10);

        public TimeSpan RetentionPeriod { get; init; } = TimeSpan.FromDays(7);

        public TimeSpan CleanupInterval { get; init; } = TimeSpan.FromDays(1);
    }
}
