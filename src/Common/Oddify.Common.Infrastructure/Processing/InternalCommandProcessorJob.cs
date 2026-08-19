using System.Data.Common;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Infrastructure.Serialization;
using Quartz;

namespace Oddify.Common.Infrastructure.Processing
{
#pragma warning disable S2077
    [DisallowConcurrentExecution]
    internal class InternalCommandProcessorJob : IJob
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public InternalCommandProcessorJob(IDbConnectionFactory dbConnectionFactory, IServiceScopeFactory serviceScopeFactory)
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
                 FROM {schema}.internal_commands
                 WHERE processed_on_utc IS NULL
                 ORDER BY enqueued_on_utc
                 """;

            List<InternalCommandRow> commands = (await connection.QueryAsync<InternalCommandRow>(
                new CommandDefinition(selectSql, cancellationToken: cancellationToken))).AsList();

            foreach (InternalCommandRow command in commands)
            {
                await ProcessCommandAsync(connection, schema, command, cancellationToken);
            }
        }

        private async Task ProcessCommandAsync(DbConnection connection, string schema, InternalCommandRow row, CancellationToken cancellationToken)
        {
            Type commandType = Type.GetType(row.Type) ?? throw new InvalidOperationException($"Unknown command type '{row.Type}'.");

            var command = (ICommand)JsonSerializer.Deserialize(row.Content, commandType, EventSerializerOptions.Instance)!;

            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.Send(command, cancellationToken);

            await MarkAsProcessedAsync(connection, schema, row.Id, cancellationToken);
        }

        private static async Task MarkAsProcessedAsync(DbConnection connection, string schema, Guid id, CancellationToken cancellationToken)
        {
            string sql = $"UPDATE {schema}.internal_commands SET processed_on_utc = now() WHERE id = @Id";

            await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        }

        private record InternalCommandRow(Guid Id, string Type, string Content);
    }
#pragma warning restore S2077
}
