using System.Data.Common;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.Infrastructure.Inbox
{
    internal class IntegrationEventGenericHandler<TIntegrationEvent> : IntegrationEventHandler<TIntegrationEvent>
        where TIntegrationEvent : IIntegrationEvent
    {
        private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IServiceProvider _serviceProvider;

        public IntegrationEventGenericHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override async Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();

            IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

            await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

            const string sql =
                "INSERT INTO users.inbox_messages(id, type, content, occurred_on_utc) VALUES (@Id, @Type, @Content::jsonb, @OccurredOnUtc)";

            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                integrationEvent.Id,
                Type = typeof(TIntegrationEvent).AssemblyQualifiedName,
                Content = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
                integrationEvent.OccurredOnUtc
            }, cancellationToken: cancellationToken));
        }
    }
}
