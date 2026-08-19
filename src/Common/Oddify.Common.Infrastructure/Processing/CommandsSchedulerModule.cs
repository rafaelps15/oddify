namespace Oddify.Common.Infrastructure.Processing
{
    // Um por módulo que agenda Commands — registrado por AddCommandsProcessor (chamado de dentro do
    // próprio composition root do módulo, não montado como array em Program.cs), resolvido como
    // IEnumerable<CommandsSchedulerModule> só pra OutboxCleanupBackgroundService saber quais schemas
    // limpar (mesmo papel de OutboxModule/InboxModule).
    public class CommandsSchedulerModule
    {
        public CommandsSchedulerModule(string schema)
        {
            Schema = schema;
        }

        public string Schema { get; }
    }
}
