namespace Oddify.Common.Infrastructure.Inbox
{
    public class InboxModule
    {
        public InboxModule(string schema)
        {
            Schema = schema;
        }

        public string Schema { get; }
    }
}
