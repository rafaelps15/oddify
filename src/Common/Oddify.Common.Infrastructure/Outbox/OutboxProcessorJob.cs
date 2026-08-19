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

namespace Oddify.Common.Infrastructure.Outbox
{
#pragma warning disable S2077
    [DisallowConcurrentExecution]
    internal class OutboxProcessorJob : IJob
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public OutboxProcessorJob(IDbConnectionFactory dbConnectionFactory, IServiceScopeFactory serviceScopeFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            string schema = context.MergedJobDataMap.GetString("Schema")!;
            CancellationToken cancellationToken = context.CancellationToken;

            await using DbConnection connection = await _dbConnectionFactory.OpenConnectionAsync();

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
            Type eventType = Type.GetType(message.Type) ?? throw new InvalidOperationException($"Unknown event type '{message.Type}'.");

            if (typeof(IIntegrationEvent).IsAssignableFrom(eventType))
            {
                var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

                await InMemoryEventBus.Instance.PublishAsync(integrationEvent, eventType, cancellationToken);
            }
            else
            {
                var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Content, eventType, EventSerializerOptions.Instance)!;

                using IServiceScope scope = _serviceScopeFactory.CreateScope();

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

        private record OutboxMessageRow(Guid Id, string Type, string Content);
    }
#pragma warning restore S2077
}
