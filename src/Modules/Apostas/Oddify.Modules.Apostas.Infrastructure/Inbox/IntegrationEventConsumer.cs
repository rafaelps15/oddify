using System.Data.Common;
using System.Text.Json;
using Dapper;
using MassTransit;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Apostas.Infrastructure.Inbox;

// Consumer MassTransit real (o único que efetivamente recebe do bus) — genérico sobre qualquer
// IIntegrationEvent que este módulo consome. Não processa nada: só grava a mensagem em
// apostas.inbox_messages e retorna — dá durabilidade do lado de quem recebe antes de qualquer
// lógica de negócio rodar. ProcessInboxJob processa de verdade, de forma assíncrona. Schema fixo
// (não parametrizado via DI) de propósito — evita um problema de ordem de registro entre
// AddMassTransit (roda dentro de Common.Infrastructure.AddInfrastructure) e o composition root do
// próprio módulo (roda depois); ver arquivo equivalente em Users.
internal sealed class IntegrationEventConsumer<TIntegrationEvent>(IDbConnectionFactory dbConnectionFactory)
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent integrationEvent = context.Message;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // ::jsonb explícito — Npgsql manda um parâmetro string como "text" por padrão, e a coluna
        // content é jsonb; sem o cast, o INSERT falha com "column is of type jsonb but expression
        // is of type text" (confirmado em teste real, não é hipotético).
        const string sql =
            "INSERT INTO apostas.inbox_messages(id, type, content, occurred_on_utc, retry_count) VALUES (@Id, @Type, @Content::jsonb, @OccurredOnUtc, 0)";

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            integrationEvent.Id,
            Type = typeof(TIntegrationEvent).AssemblyQualifiedName,
            Content = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            integrationEvent.OccurredOnUtc
        }, cancellationToken: context.CancellationToken));
    }
}
