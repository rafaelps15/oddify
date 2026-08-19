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

namespace Oddify.Common.Infrastructure.Outbox;

// "Arquivar" simplificado pra apagar — mensagens/comandos processados com mais tempo que
// RetentionPeriod só ocupam espaço e mantêm o conteúdo bruto em texto claro mais tempo do que
// precisa. Um único serviço cuida da limpeza de outbox_messages, inbox_messages E internal_commands
// de todos os módulos (uma tabela por schema cada), em vez de um serviço por módulo/tabela.
//
// S2077 desabilitado: {schema} vem só de OutboxModule/InboxModule/CommandsSchedulerModule (código
// no host), nunca de request — CutoffUtc continua parametrizado via Dapper normalmente.
#pragma warning disable S2077
internal sealed partial class OutboxCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    IEnumerable<OutboxModule> outboxModules,
    IEnumerable<InboxModule> inboxModules,
    IEnumerable<CommandsSchedulerModule> commandsSchedulerModules,
    ILogger<OutboxCleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.CleanupInterval);

        do
        {
            foreach (OutboxModule module in outboxModules)
            {
                await CleanupAsync(module.Schema, "outbox_messages", stoppingToken);
            }

            foreach (InboxModule module in inboxModules)
            {
                await CleanupAsync(module.Schema, "inbox_messages", stoppingToken);
            }

            foreach (CommandsSchedulerModule module in commandsSchedulerModules)
            {
                await CleanupAsync(module.Schema, "internal_commands", stoppingToken);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(string schema, string table, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Schema/table vêm só de OutboxModule/InboxModule (registrado em código no host) — mesmo
        // raciocínio de segurança do OutboxProcessorJob.
        string sql = $"DELETE FROM {schema}.{table} WHERE processed_on_utc < @CutoffUtc";

        try
        {
            int deleted = await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { CutoffUtc = DateTime.UtcNow - options.Value.RetentionPeriod },
                cancellationToken: cancellationToken));

            LogCleanupCompleted(logger, schema, table, deleted);
        }
        catch (Exception exception)
        {
            LogCleanupFailed(logger, schema, table, exception);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Cleanup ({Schema}.{Table}) removeu {Count} mensagens processadas")]
    private static partial void LogCleanupCompleted(ILogger logger, string schema, string table, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Falha ao limpar mensagens processadas ({Schema}.{Table})")]
    private static partial void LogCleanupFailed(ILogger logger, string schema, string table, Exception exception);
}
#pragma warning restore S2077
