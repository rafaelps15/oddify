using System.Data.Common;
using System.Text.Json;
using Dapper;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Outbox;
using Oddify.Common.Domain;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

// Job periódico do Quartz, uma instância por módulo (JobKey/JobDataMap diferentes, ver
// AddOutboxProcessing) — lê um lote de mensagens pendentes da tabela outbox_messages do schema
// daquele módulo. A grande maioria das linhas é um domain event (capturado automaticamente por
// InsertOutboxMessagesInterceptor) — para essas, publica via IPublisher.Publish, que o MediatR já
// resolve sozinho pra cada IDomainEventHandler<T>/INotificationHandler<T> registrado (assembly
// scanning padrão do AddMediatR, nenhum registro/reflexão própria necessária); se o handler
// precisa notificar outro módulo, ele mesmo chama IEventBus.PublishAsync(...) de dentro do seu
// Handle. Uma minoria é um integration event gravado explicitamente via IOutboxWriter (código que
// não é ele mesmo despachado por este job, ex. um CommandHandler comum) — para essas, publica
// direto no bus via IPublishEndpoint, sem passar pelo MediatR.
//
// S2077 desabilitado neste arquivo: cada SQL interpola {schema}, nunca um valor de request — vem
// só de OutboxModule, registrado em código no host (Program.cs), nunca de entrada externa. Cada
// valor que É de request (Id, RetryCount, Error, Exhausted, BatchSize) continua parametrizado via
// Dapper normalmente.
#pragma warning disable S2077
[DisallowConcurrentExecution]
internal sealed partial class OutboxProcessorJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    IPublishEndpoint publishEndpoint,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxProcessorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string schema = context.MergedJobDataMap.GetString("Schema")!;
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Schema vem só de OutboxModule (código do host, nunca de request) — seguro interpolar.
        string selectSql =
            $"""
             SELECT id AS Id, type AS Type, content AS Content, retry_count AS RetryCount
             FROM {schema}.outbox_messages
             WHERE processed_on_utc IS NULL AND failed_at_utc IS NULL
             ORDER BY occurred_on_utc
             LIMIT @BatchSize
             FOR UPDATE SKIP LOCKED
             """;

        var command = new CommandDefinition(selectSql, new { options.Value.BatchSize }, transaction, cancellationToken: cancellationToken);
        List<OutboxMessageRow> messages = (await connection.QueryAsync<OutboxMessageRow>(command)).AsList();

        foreach (OutboxMessageRow message in messages)
        {
            await ProcessMessageAsync(connection, transaction, schema, message, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        DbTransaction transaction,
        string schema,
        OutboxMessageRow message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Type é o AssemblyQualifiedName completo (gravado por InsertOutboxMessagesInterceptor
            // ou EfOutboxWriter) — resolve direto pelo CLR.
            var eventType = Type.GetType(message.Type);

            if (eventType is null)
            {
                throw new InvalidOperationException($"Unknown event type '{message.Type}'.");
            }

            if (typeof(IIntegrationEvent).IsAssignableFrom(eventType))
            {
                // Enfileirado explicitamente via IOutboxWriter, não é um domain event capturado
                // pelo interceptor — publica direto no bus, nunca pelo MediatR.
                object integrationEvent = JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

                await publishEndpoint.Publish(integrationEvent, eventType, cancellationToken);
            }
            else
            {
                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

                using IServiceScope scope = serviceScopeFactory.CreateScope();

                IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                await publisher.Publish(domainEvent, cancellationToken);
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
        string sql = $"UPDATE {schema}.outbox_messages SET processed_on_utc = now() WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }

    private async Task MarkAsFailedAsync(
        DbConnection connection,
        DbTransaction transaction,
        string schema,
        OutboxMessageRow message,
        string error,
        CancellationToken cancellationToken)
    {
        int retryCount = message.RetryCount + 1;
        bool exhausted = retryCount >= options.Value.MaxAttempts;

        string sql =
            $"""
             UPDATE {schema}.outbox_messages
             SET retry_count = @RetryCount, error = @Error, failed_at_utc = CASE WHEN @Exhausted THEN now() ELSE NULL END
             WHERE id = @Id
             """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { message.Id, RetryCount = retryCount, Error = error, Exhausted = exhausted },
            transaction,
            cancellationToken: cancellationToken));
    }

    private sealed record OutboxMessageRow(Guid Id, string Type, string Content, int RetryCount);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Falha ao processar a outbox message {MessageId}")]
    private static partial void LogMessageFailed(ILogger logger, Guid messageId, Exception exception);
}
#pragma warning restore S2077
