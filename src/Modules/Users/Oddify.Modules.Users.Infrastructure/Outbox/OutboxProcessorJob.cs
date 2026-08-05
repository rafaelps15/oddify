using System.Data.Common;
using System.Text.Json;
using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Data;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.IntegrationEvents;
using Quartz;

namespace Oddify.Modules.Users.Infrastructure.Outbox;

// Job periódico do Quartz: lê um lote de mensagens pendentes, desserializa via assembly
// conhecida (Users.IntegrationEvents.AssemblyReference) e publica com IPublishEndpoint — quem
// reage de verdade (mandar e-mail, notificar Apostas) é sempre um consumer do lado de lá, nunca
// este job.
[DisallowConcurrentExecution]
internal sealed partial class OutboxProcessorJob(
    IDbConnectionFactory dbConnectionFactory,
    IPublishEndpoint publishEndpoint,
    IOptions<OutboxProcessorOptions> options,
    ILogger<OutboxProcessorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        // FOR UPDATE SKIP LOCKED trava as linhas do lote pra outra instância do job não pegar as
        // mesmas — só é estritamente necessário com mais de um worker rodando, mas o custo com
        // um só é desprezível.
        const string selectSql =
            """
            SELECT id AS Id, type AS Type, content AS Content, retry_count AS RetryCount
            FROM users.outbox_messages
            WHERE processed_on_utc IS NULL AND failed_at_utc IS NULL
            ORDER BY occurred_on_utc
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
            """;

        var command = new CommandDefinition(selectSql, new { options.Value.BatchSize }, transaction, cancellationToken: cancellationToken);
        List<OutboxMessageRow> messages = (await connection.QueryAsync<OutboxMessageRow>(command)).AsList();

        foreach (OutboxMessageRow message in messages)
        {
            await ProcessMessageAsync(connection, transaction, message, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        DbTransaction transaction,
        OutboxMessageRow message,
        CancellationToken cancellationToken)
    {
        try
        {
            Type? messageType = AssemblyReference.Assembly.GetType(message.Type);
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

            await MarkAsProcessedAsync(connection, transaction, message.Id, cancellationToken);
        }
        catch (Exception exception)
        {
            LogMessageFailed(logger, message.Id, exception);

            await MarkAsFailedAsync(connection, transaction, message, exception.Message, cancellationToken);
        }
    }

    private static async Task MarkAsProcessedAsync(DbConnection connection, DbTransaction transaction, Guid id, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE users.outbox_messages SET processed_on_utc = now() WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, transaction, cancellationToken: cancellationToken));
    }

    private async Task MarkAsFailedAsync(
        DbConnection connection,
        DbTransaction transaction,
        OutboxMessageRow message,
        string error,
        CancellationToken cancellationToken)
    {
        int retryCount = message.RetryCount + 1;
        bool exhausted = retryCount >= options.Value.MaxAttempts;

        const string sql =
            """
            UPDATE users.outbox_messages
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
