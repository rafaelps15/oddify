using System.Data.Common;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Domain;
using Oddify.Common.Infrastructure.EventBus;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

// Job periódico do Quartz, uma instância por módulo (JobKey/JobDataMap diferentes, ver
// AddOutboxProcessor) — lê as mensagens pendentes da outbox daquele schema, na ordem em que
// ocorreram. Espelha ProcessOutboxCommandHandler do projeto de referência (Modular Monolith with
// DDD): sem lote/lock/retry — cada linha é lida, despachada e marcada como processada, uma de cada
// vez; se uma falhar, a exceção sobe e a mensagem continua pendente pra próxima rodada do job (as
// que já foram marcadas antes dela continuam processadas).
//
// A maioria das linhas é um domain event (capturado automaticamente por
// InsertOutboxMessagesInterceptor) — para essas, publica via IPublisher.Publish, que o MediatR
// resolve sozinho pra cada IDomainEventHandler<T>/INotificationHandler<T> registrado; se o handler
// precisa notificar outro módulo, ele mesmo chama IEventBus.PublishAsync(...) de dentro do seu
// Handle. Uma minoria é um integration event gravado explicitamente via IOutboxWriter — para essas,
// publica direto no InMemoryEventBus, sem passar pelo MediatR.
//
// S2077 desabilitado neste arquivo: {schema} vem só de OutboxModule, registrado em código no host
// (Program.cs), nunca de entrada externa. Id continua parametrizado via Dapper normalmente.
#pragma warning disable S2077
[DisallowConcurrentExecution]
internal sealed class OutboxProcessorJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string schema = context.MergedJobDataMap.GetString("Schema")!;
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // Schema vem só de OutboxModule (código do host, nunca de request) — seguro interpolar.
        string selectSql =
            $"""
             SELECT id AS Id, type AS Type, content AS Content
             FROM {schema}.outbox_messages
             WHERE processed_on_utc IS NULL
             ORDER BY occurred_on_utc
             """;

        List<OutboxMessageRow> messages = (await connection.QueryAsync<OutboxMessageRow>(
            new CommandDefinition(selectSql, cancellationToken: cancellationToken))).AsList();

        foreach (OutboxMessageRow message in messages)
        {
            await ProcessMessageAsync(connection, schema, message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(DbConnection connection, string schema, OutboxMessageRow message, CancellationToken cancellationToken)
    {
        // Type é o AssemblyQualifiedName completo (gravado por InsertOutboxMessagesInterceptor ou
        // EfOutboxWriter) — resolve direto pelo CLR.
        Type eventType = Type.GetType(message.Type) ?? throw new InvalidOperationException($"Unknown event type '{message.Type}'.");

        if (typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        {
            // Enfileirado explicitamente via IOutboxWriter, não é um domain event capturado pelo
            // interceptor — publica direto no bus, nunca pelo MediatR.
            var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

            await InMemoryEventBus.Instance.PublishAsync(integrationEvent, eventType, cancellationToken);
        }
        else
        {
            var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

            using IServiceScope scope = serviceScopeFactory.CreateScope();

            IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(domainEvent, cancellationToken);
        }

        await MarkAsProcessedAsync(connection, schema, message.Id, cancellationToken);
    }

    private static async Task MarkAsProcessedAsync(DbConnection connection, string schema, Guid id, CancellationToken cancellationToken)
    {
        string sql = $"UPDATE {schema}.outbox_messages SET processed_on_utc = now() WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private sealed record OutboxMessageRow(Guid Id, string Type, string Content);
}
#pragma warning restore S2077
