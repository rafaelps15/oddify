using System.Data.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Modules.Users.Application.Abstractions.Outbox;

namespace Oddify.Modules.Users.Infrastructure.Outbox;

// "Arquivar" simplificado pra apagar — mensagens processadas com mais tempo que RetentionPeriod
// só ocupam espaço e mantêm o token bruto (ver IOutboxWriter/SendVerificationEmailIntegrationEvent)
// mais tempo do que precisa em texto claro. Se um dia precisar de arquivamento de verdade, é uma
// extensão pequena sobre este job.
internal sealed partial class OutboxCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxProcessorOptions> options,
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
            await CleanupAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql = "DELETE FROM users.outbox_messages WHERE processed_on_utc < @CutoffUtc";

        try
        {
            int deleted = await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { CutoffUtc = DateTime.UtcNow - options.Value.RetentionPeriod },
                cancellationToken: cancellationToken));

            LogCleanupCompleted(logger, deleted);
        }
        catch (Exception exception)
        {
            LogCleanupFailed(logger, exception);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Outbox cleanup removeu {Count} mensagens processadas")]
    private static partial void LogCleanupCompleted(ILogger logger, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Falha ao limpar mensagens processadas da outbox")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception);
}
