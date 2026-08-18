using System.Data.Common;
using System.Text.Json;
using Dapper;
using MassTransit;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.Infrastructure.Inbox;

// Consumer MassTransit real — ver comentário equivalente em Apostas.Infrastructure/Inbox
// (mesmo desenho, schema fixo pra evitar o problema de ordem de registro com AddMassTransit).
internal sealed class IntegrationEventConsumer<TIntegrationEvent>(IDbConnectionFactory dbConnectionFactory)
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent integrationEvent = context.Message;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // ::jsonb explícito — ver comentário equivalente em Apostas.Infrastructure/Inbox.
        const string sql =
            "INSERT INTO users.inbox_messages(id, type, content, occurred_on_utc, retry_count) VALUES (@Id, @Type, @Content::jsonb, @OccurredOnUtc, 0)";

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            integrationEvent.Id,
            Type = typeof(TIntegrationEvent).AssemblyQualifiedName,
            Content = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            integrationEvent.OccurredOnUtc
        }, cancellationToken: context.CancellationToken));
    }
}
