using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Outbox;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox;

// Espelho de OutboxProcessorJob — uma instância por módulo que consome integration event de
// outro módulo. Lê um lote de inbox_messages pendentes, resolve cada IIntegrationEventHandler<T>
// local (IntegrationEventHandlersFactory, no assembly Presentation do módulo) e invoca.
//
// S2077 desabilitado neste arquivo: {schema} vem só de InboxModule (código do host), nunca de
// request — Id/RetryCount/Error/Exhausted/BatchSize continuam parametrizados via Dapper.
#pragma warning disable S2077
[DisallowConcurrentExecution]
internal sealed partial class ProcessInboxJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxProcessorOptions> options,
    ILogger<ProcessInboxJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string schema = context.MergedJobDataMap.GetString("Schema")!;
        var presentationAssembly = Assembly.Load(context.MergedJobDataMap.GetString("PresentationAssembly")!);
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        string selectSql =
            $"""
             SELECT id AS Id, type AS Type, content AS Content, retry_count AS RetryCount
             FROM {schema}.inbox_messages
             WHERE processed_on_utc IS NULL AND failed_at_utc IS NULL
             ORDER BY occurred_on_utc
             LIMIT @BatchSize
             FOR UPDATE SKIP LOCKED
             """;

        var command = new CommandDefinition(selectSql, new { options.Value.BatchSize }, transaction, cancellationToken: cancellationToken);
        List<InboxMessageRow> messages = (await connection.QueryAsync<InboxMessageRow>(command)).AsList();

        foreach (InboxMessageRow message in messages)
        {
            await ProcessMessageAsync(connection, transaction, schema, presentationAssembly, message, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        DbTransaction transaction,
        string schema,
        Assembly presentationAssembly,
        InboxMessageRow message,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEventType = Type.GetType(message.Type);

            if (integrationEventType is null)
            {
                throw new InvalidOperationException($"Unknown integration event type '{message.Type}'.");
            }

            var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Content, integrationEventType, EventSerializerOptions.Instance)!;

            using IServiceScope scope = serviceScopeFactory.CreateScope();

            IEnumerable<IIntegrationEventHandler> handlers =
                IntegrationEventHandlersFactory.GetHandlers(integrationEventType, scope.ServiceProvider, presentationAssembly);

            foreach (IIntegrationEventHandler handler in handlers)
            {
                await handler.Handle(integrationEvent, cancellationToken);
            }

            await MarkAsProcessedAsync(connection, transaction, schema, message.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            LogMessageFailed(logger, message.Id, exception);

            await MarkAsFailedAsync(connection, transaction, schema, message, exception.Message, cancellationToken);
        }
    }

    private static async Task MarkAsProcessedAsync(DbConnection connection, DbTransaction transaction, string schema, Guid id, CancellationToken cancellationToken)
    {
        string sql = $"UPDATE {schema}.inbox_messages SET processed_on_utc = now() WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }

    private async Task MarkAsFailedAsync(
        DbConnection connection,
        DbTransaction transaction,
        string schema,
        InboxMessageRow message,
        string error,
        CancellationToken cancellationToken)
    {
        int retryCount = message.RetryCount + 1;
        bool exhausted = retryCount >= options.Value.MaxAttempts;

        string sql =
            $"""
             UPDATE {schema}.inbox_messages
             SET retry_count = @RetryCount, error = @Error, failed_at_utc = CASE WHEN @Exhausted THEN now() ELSE NULL END
             WHERE id = @Id
             """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { message.Id, RetryCount = retryCount, Error = error, Exhausted = exhausted },
            transaction,
            cancellationToken: cancellationToken));
    }

    private sealed record InboxMessageRow(Guid Id, string Type, string Content, int RetryCount);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Falha ao processar a inbox message {MessageId}")]
    private static partial void LogMessageFailed(ILogger logger, Guid messageId, Exception exception);
}
#pragma warning restore S2077
