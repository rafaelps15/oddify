using System.Text.Json;
using Oddify.Common.Application.EventBus;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.Infrastructure.Database;

namespace Oddify.Modules.Users.Infrastructure.Outbox;

internal sealed class OutboxWriter(UsersDbContext context) : IOutboxWriter
{
    public void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent
    {
        var message = OutboxMessage.Create(
            typeof(T).FullName!,
            JsonSerializer.Serialize(integrationEvent),
            integrationEvent.OccurredOnUtc);

        context.Set<OutboxMessage>().Add(message);
    }
}
