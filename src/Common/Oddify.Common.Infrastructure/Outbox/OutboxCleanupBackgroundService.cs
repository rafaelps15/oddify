using System.Data.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Outbox;

namespace Oddify.Common.Infrastructure.Outbox;

// "Arquivar" simplificado pra apagar — mensagens processadas com mais tempo que RetentionPeriod
// só ocupam espaço e mantêm o conteúdo bruto do evento em texto claro mais tempo do que precisa.
// Um único serviço cuida da limpeza de todos os módulos com outbox (uma tabela por schema), em
// vez de um serviço por módulo.
//
// S2077 desabilitado: {schema} vem só de OutboxModule (código no host), nunca de request —
// CutoffUtc continua parametrizado via Dapper normalmente.
#pragma warning disable S2077
internal sealed partial class OutboxCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
    OutboxModule[] outboxModules,
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
                await CleanupAsync(module.Schema, stoppingToken);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(string schema, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Schema vem só de OutboxModule (registrado em código no host) — mesmo raciocínio de
        // segurança do OutboxProcessorJob.
        string sql = $"DELETE FROM {schema}.outbox_messages WHERE processed_on_utc < @CutoffUtc";

        try
        {
            int deleted = await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { CutoffUtc = DateTime.UtcNow - options.Value.RetentionPeriod },
                cancellationToken: cancellationToken));

            LogCleanupCompleted(logger, schema, deleted);
        }
        catch (Exception exception)
        {
            LogCleanupFailed(logger, schema, exception);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Outbox cleanup ({Schema}) removeu {Count} mensagens processadas")]
    private static partial void LogCleanupCompleted(ILogger logger, string schema, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Falha ao limpar mensagens processadas da outbox ({Schema})")]
    private static partial void LogCleanupFailed(ILogger logger, string schema, Exception exception);
}
#pragma warning restore S2077
