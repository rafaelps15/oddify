using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

// Job periódico do Quartz, uma instância por módulo com outbox (JobKey/JobDataMap diferentes por
// módulo, ver AddOutboxProcessing) — lê um lote de mensagens pendentes da tabela outbox_messages
// do schema daquele módulo, desserializa via a assembly de IntegrationEvents daquele módulo e
// publica com IPublishEndpoint. Quem reage de verdade (mandar e-mail, notificar outro módulo) é
// sempre um consumer do lado de lá, nunca este job.
//
// S2077 desabilitado neste arquivo: cada SQL interpola {schema}, nunca um valor de request — vem
// só de OutboxModule, registrado em código no host (Program.cs), nunca de entrada externa. Cada
// valor que É de request (Id, RetryCount, Error, Exhausted, BatchSize) continua parametrizado via
// Dapper normalmente.
#pragma warning disable S2077
[DisallowConcurrentExecution]
internal sealed partial class OutboxProcessorJob(
    IDbConnectionFactory dbConnectionFactory,
    IPublishEndpoint publishEndpoint,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxProcessorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string schema = context.MergedJobDataMap.GetString("Schema")!;
        var messageAssembly = Assembly.Load(context.MergedJobDataMap.GetString("MessageAssembly")!);
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Schema vem só de OutboxModule (registrado em código no host, nunca de entrada externa)
        // — interpolar aqui é seguro pelo mesmo motivo que nameof(...) é seguro em SQL de query
        // handler: não é valor de request. FOR UPDATE SKIP LOCKED só é estritamente necessário
        // com mais de um worker rodando, mas o custo com um só é desprezível.
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
            await ProcessMessageAsync(connection, transaction, schema, messageAssembly, message, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        DbTransaction transaction,
        string schema,
        Assembly messageAssembly,
        OutboxMessageRow message,
        CancellationToken cancellationToken)
    {
        try
        {
            Type? messageType = messageAssembly.GetType(message.Type);
            if (messageType is null)
            {
                throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'.");
            }

            object? deserializedMessage = JsonSerializer.Deserialize(message.Content, messageType);
            if (deserializedMessage is null)
            {
                throw new InvalidOperationException($"Failed to deserialize outbox message '{message.Id}'.");
            }

            await publishEndpoint.Publish(deserializedMessage, messageType, cancellationToken);

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
