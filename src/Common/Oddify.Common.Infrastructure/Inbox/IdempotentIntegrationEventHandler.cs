using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.Inbox;

// Espelho de IdempotentDomainEventHandler, pro lado de quem consome — checa
// inbox_message_consumers (schema do módulo dono) antes de rodar o handler de verdade.
//
// S2077 desabilitado: {schema} vem só de código do host, nunca de request.
#pragma warning disable S2077
internal sealed class IdempotentIntegrationEventHandler<TIntegrationEvent>(
    IIntegrationEventHandler<TIntegrationEvent> decorated,
    IDbConnectionFactory dbConnectionFactory,
    string schema)
    : IntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    public override async Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var consumer = new InboxMessageConsumer(integrationEvent.Id, decorated.GetType().Name);

        if (await ConsumerExistsAsync(connection, consumer, cancellationToken))
        {
            return;
        }

        await decorated.Handle(integrationEvent, cancellationToken);

        await InsertConsumerAsync(connection, consumer, cancellationToken);
    }

    private async Task<bool> ConsumerExistsAsync(DbConnection connection, InboxMessageConsumer consumer, CancellationToken cancellationToken)
    {
        string sql =
            $"""
             SELECT EXISTS(
                 SELECT 1 FROM {schema}.inbox_message_consumers
                 WHERE inbox_message_id = @InboxMessageId AND name = @Name
             )
             """;

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, consumer, cancellationToken: cancellationToken));
    }

    private async Task InsertConsumerAsync(DbConnection connection, InboxMessageConsumer consumer, CancellationToken cancellationToken)
    {
        string sql = $"INSERT INTO {schema}.inbox_message_consumers(inbox_message_id, name) VALUES (@InboxMessageId, @Name)";

        await connection.ExecuteAsync(new CommandDefinition(sql, consumer, cancellationToken: cancellationToken));
    }
}
#pragma warning restore S2077
