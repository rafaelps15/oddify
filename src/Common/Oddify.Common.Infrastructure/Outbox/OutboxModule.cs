namespace Oddify.Common.Infrastructure.Outbox
{
    public class OutboxModule
    {
        public OutboxModule(string schema)
        {
            Schema = schema;
        }

        public string Schema { get; }
    }
}
