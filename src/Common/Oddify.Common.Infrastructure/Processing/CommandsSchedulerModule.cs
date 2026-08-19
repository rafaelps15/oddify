namespace Oddify.Common.Infrastructure.Processing
{
    public class CommandsSchedulerModule
    {
        public CommandsSchedulerModule(string schema)
        {
            Schema = schema;
        }

        public string Schema { get; }
    }
}
