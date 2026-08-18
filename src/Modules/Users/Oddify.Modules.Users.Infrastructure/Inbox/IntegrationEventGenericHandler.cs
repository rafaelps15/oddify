using System.Data.Common;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.Infrastructure.Inbox;

// O único handler que efetivamente assina o InMemoryEventBus — ver comentário equivalente em
// Apostas.Infrastructure/Inbox.
internal sealed class IntegrationEventGenericHandler<TIntegrationEvent>(IServiceProvider serviceProvider)
    : IntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public override async Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // ::jsonb explícito — ver comentário equivalente em Apostas.Infrastructure/Inbox.
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
