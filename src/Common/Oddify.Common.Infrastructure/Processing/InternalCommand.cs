namespace Oddify.Common.Infrastructure.Processing
{
    public class InternalCommand
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public DateTime EnqueuedOnUtc { get; set; }

        public DateTime? ProcessedOnUtc { get; set; }

        public InternalCommand(string type, string content, DateTime enqueuedOnUtc)
        {
            Id = Guid.NewGuid();
            Type = type;
            Content = content;
            EnqueuedOnUtc = enqueuedOnUtc;
        }

        private InternalCommand()
        {
        }
    }
}
