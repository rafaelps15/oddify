using System.Data.Common;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Analise.Infrastructure.Inbox;

// O único handler que efetivamente assina o InMemoryEventBus (ver AnaliseModule.Initialize) —
// genérico sobre qualquer IIntegrationEvent que este módulo consome. Não processa nada: só grava a
// mensagem em analise.inbox_messages e retorna — dá durabilidade do lado de quem recebe antes de
// qualquer lógica de negócio rodar. ProcessInboxJob processa de verdade, de forma assíncrona.
// Recebe o IServiceProvider raiz (não um serviço escopado) porque é construído uma única vez no
// startup e reutilizado pelo tempo de vida do processo — cada Handle abre seu próprio escopo.
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

        // ::jsonb explícito — Npgsql manda um parâmetro string como "text" por padrão, e a coluna
        // content é jsonb; sem o cast, o INSERT falha com "column is of type jsonb but expression
        // is of type text".
        const string sql =
            "INSERT INTO analise.inbox_messages(id, type, content, occurred_on_utc) VALUES (@Id, @Type, @Content::jsonb, @OccurredOnUtc)";

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            integrationEvent.Id,
            Type = typeof(TIntegrationEvent).AssemblyQualifiedName,
            Content = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            integrationEvent.OccurredOnUtc
        }, cancellationToken: cancellationToken));
    }
}
