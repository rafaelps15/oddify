using System.Data.Common;
using System.Reflection;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox
{
#pragma warning disable S2077
    [DisallowConcurrentExecution]
    internal class ProcessInboxJob : IJob
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ProcessInboxJob> _logger;

        public ProcessInboxJob(IDbConnectionFactory dbConnectionFactory, IServiceScopeFactory serviceScopeFactory, ILogger<ProcessInboxJob> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            string schema = context.MergedJobDataMap.GetString("Schema")!;
            var presentationAssembly = Assembly.Load(context.MergedJobDataMap.GetString("PresentationAssembly")!);
            CancellationToken cancellationToken = context.CancellationToken;

            await using DbConnection connection = await _dbConnectionFactory.OpenConnectionAsync();

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
                try
                {
                    await ProcessMessageAsync(connection, schema, presentationAssembly, message, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Uma mensagem com falha (ex.: conflito de concorrência no handler, evento
                    // malformado) não deve travar o lote inteiro — as demais mensagens pendentes
                    // (deste schema e, sem isso, potencialmente de qualquer outro módulo no mesmo
                    // ciclo) continuam sendo processadas. A mensagem que falhou não é marcada como
                    // processada, então será retentada no próximo ciclo do job.
                    _logger.LogError(ex, "Falha ao processar a mensagem {MessageId} do inbox de {Schema}", message.Id, schema);
                }
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

            using IServiceScope scope = _serviceScopeFactory.CreateScope();

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

        private record InboxMessageRow(Guid Id, string Type, string Content);
    }
#pragma warning restore S2077
}
