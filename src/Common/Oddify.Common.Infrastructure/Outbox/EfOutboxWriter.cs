using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Outbox;
using Oddify.Common.Infrastructure.Serialization;

namespace Oddify.Common.Infrastructure.Outbox
{
    internal class EfOutboxWriter<TContext> : IOutboxWriter
        where TContext : DbContext
    {
        private readonly TContext _context;

        public EfOutboxWriter(TContext context)
        {
            _context = context;
        }

        public void Enqueue<T>(T integrationEvent) where T : IIntegrationEvent
        {
            var message = new OutboxMessage(
                integrationEvent.Id,
                typeof(T).AssemblyQualifiedName!,
                JsonSerializer.Serialize(integrationEvent, EventSerializerOptions.Instance),
                integrationEvent.OccurredOnUtc);

            _context.Set<OutboxMessage>().Add(message);
        }
    }

    public static class OutboxWriterServiceCollectionExtensions
    {
        public static IServiceCollection AddOutboxWriter<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped<IOutboxWriter, EfOutboxWriter<TContext>>();
            return services;
        }
    }
}
