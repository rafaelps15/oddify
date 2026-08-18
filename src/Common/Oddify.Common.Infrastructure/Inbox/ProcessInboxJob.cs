using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox;

// Espelho de OutboxProcessorJob — uma instância por módulo que consome integration event de outro
// módulo. Lê as inbox_messages pendentes, resolve e invoca cada IIntegrationEventHandler<T> local
// (IntegrationEventHandlersFactory, no assembly Presentation do módulo), marca processado. Sem
// lote/lock/retry, mesmo raciocínio de OutboxProcessorJob: se uma mensagem falhar, a exceção sobe e
// ela continua pendente pra próxima rodada.
//
// S2077 desabilitado neste arquivo: {schema} vem só de InboxModule (código do host), nunca de
// request — Id continua parametrizado via Dapper normalmente.
#pragma warning disable S2077
[DisallowConcurrentExecution]
internal sealed class ProcessInboxJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string schema = context.MergedJobDataMap.GetString("Schema")!;
        var presentationAssembly = Assembly.Load(context.MergedJobDataMap.GetString("PresentationAssembly")!);
        CancellationToken cancellationToken = context.CancellationToken;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        string selectSql =
            $"""
             SELECT id AS Id, type AS Type, content AS Content
             FROM {schema}.inbox_messages
             WHERE processed_on_utc IS NULL
             ORDER BY occurred_on_utc
             """;

        List<InboxMessageRow> messages = (await connection.QueryAsync<InboxMessageRow>(
            new CommandDefinition(selectSql, cancellationToken: cancellationToken))).AsList();

        foreach (InboxMessageRow message in messages)
        {
            await ProcessMessageAsync(connection, schema, presentationAssembly, message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(
        DbConnection connection,
        string schema,
        Assembly presentationAssembly,
        InboxMessageRow message,
        CancellationToken cancellationToken)
    {
        Type integrationEventType = Type.GetType(message.Type)
            ?? throw new InvalidOperationException($"Unknown integration event type '{message.Type}'.");

        var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Content, integrationEventType, EventSerializerOptions.Instance)!;

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        IEnumerable<IIntegrationEventHandler> handlers =
            IntegrationEventHandlersFactory.GetHandlers(integrationEventType, scope.ServiceProvider, presentationAssembly);

        foreach (IIntegrationEventHandler handler in handlers)
        {
            await handler.Handle(integrationEvent, cancellationToken);
        }

        await MarkAsProcessedAsync(connection, schema, message.Id, cancellationToken);
    }

    private static async Task MarkAsProcessedAsync(DbConnection connection, string schema, Guid id, CancellationToken cancellationToken)
    {
        string sql = $"UPDATE {schema}.inbox_messages SET processed_on_utc = now() WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private sealed record InboxMessageRow(Guid Id, string Type, string Content);
}
#pragma warning restore S2077
