using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oddify.Common.Domain;
using Oddify.Common.Infrastructure.Serialization;

namespace Oddify.Common.Infrastructure.Outbox
{
    public class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
            {
                InsertOutboxMessages(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                InsertOutboxMessages(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void InsertOutboxMessages(DbContext context)
        {
            var outboxMessages = context.ChangeTracker.Entries<Entity>()
                .Select(entry => entry.Entity)
                .SelectMany(entity =>
                {
                    IReadOnlyCollection<IDomainEvent> domainEvents = entity.DomainEvents;

                    entity.ClearDomainEvents();

                    return domainEvents;
                })
                .Select(domainEvent => new OutboxMessage(
                    domainEvent.Id,
                    domainEvent.GetType().AssemblyQualifiedName!,
                    JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), EventSerializerOptions.Instance),
                    domainEvent.OccurredOnUtc))
                .ToList();

            context.Set<OutboxMessage>().AddRange(outboxMessages);
        }
    }
}
