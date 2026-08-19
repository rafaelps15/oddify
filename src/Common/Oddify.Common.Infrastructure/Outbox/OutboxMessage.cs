namespace Oddify.Common.Infrastructure.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public DateTime OccurredOnUtc { get; set; }

        public DateTime? ProcessedOnUtc { get; set; }

        public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
        {
            Id = id;
            Type = type;
            Content = content;
            OccurredOnUtc = occurredOnUtc;
        }

        private OutboxMessage()
        {
        }
    }
}
