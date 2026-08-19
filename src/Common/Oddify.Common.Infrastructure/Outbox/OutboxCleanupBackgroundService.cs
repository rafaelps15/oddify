using System.Data.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Outbox;
using Oddify.Common.Infrastructure.Inbox;
using Oddify.Common.Infrastructure.Processing;

namespace Oddify.Common.Infrastructure.Outbox
{
#pragma warning disable S2077
    internal partial class OutboxCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<OutboxProcessorOptions> _options;
        private readonly IEnumerable<OutboxModule> _outboxModules;
        private readonly IEnumerable<InboxModule> _inboxModules;
        private readonly IEnumerable<CommandsSchedulerModule> _commandsSchedulerModules;
        private readonly ILogger<OutboxCleanupBackgroundService> _logger;

        public OutboxCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxProcessorOptions> options,
            IEnumerable<OutboxModule> outboxModules,
            IEnumerable<InboxModule> inboxModules,
            IEnumerable<CommandsSchedulerModule> commandsSchedulerModules,
            ILogger<OutboxCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _outboxModules = outboxModules;
            _inboxModules = inboxModules;
            _commandsSchedulerModules = commandsSchedulerModules;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Value.Enabled)
            {
                return;
            }

            using var timer = new PeriodicTimer(_options.Value.CleanupInterval);

            do
            {
                foreach (OutboxModule module in _outboxModules)
                {
                    await CleanupAsync(module.Schema, "outbox_messages", stoppingToken);
                }

                foreach (InboxModule module in _inboxModules)
                {
                    await CleanupAsync(module.Schema, "inbox_messages", stoppingToken);
                }

                foreach (CommandsSchedulerModule module in _commandsSchedulerModules)
                {
                    await CleanupAsync(module.Schema, "internal_commands", stoppingToken);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CleanupAsync(string schema, string table, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

            await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

            string sql = $"DELETE FROM {schema}.{table} WHERE processed_on_utc < @CutoffUtc";

            try
            {
                int deleted = await connection.ExecuteAsync(new CommandDefinition(
                    sql,
                    new { CutoffUtc = DateTime.UtcNow - _options.Value.RetentionPeriod },
                    cancellationToken: cancellationToken));

                LogCleanupCompleted(_logger, schema, table, deleted);
            }
            catch (Exception exception)
            {
                LogCleanupFailed(_logger, schema, table, exception);
            }
        }

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Cleanup ({Schema}.{Table}) removeu {Count} mensagens processadas")]
        private static partial void LogCleanupCompleted(ILogger logger, string schema, string table, int count);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Falha ao limpar mensagens processadas ({Schema}.{Table})")]
        private static partial void LogCleanupFailed(ILogger logger, string schema, string table, Exception exception);
    }
#pragma warning restore S2077
}
