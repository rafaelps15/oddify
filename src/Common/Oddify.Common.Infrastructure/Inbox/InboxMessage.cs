namespace Oddify.Common.Infrastructure.Inbox
{
    public class InboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public DateTime OccurredOnUtc { get; set; }

        public DateTime? ProcessedOnUtc { get; set; }

        public InboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
        {
            Id = id;
            Type = type;
            Content = content;
            OccurredOnUtc = occurredOnUtc;
        }

        private InboxMessage()
        {
        }
    }
}
