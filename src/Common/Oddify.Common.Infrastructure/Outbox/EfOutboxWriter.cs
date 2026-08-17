using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Outbox;

namespace Oddify.Common.Infrastructure.Outbox;

internal sealed class EfOutboxWriter<TContext>(TContext context) : IOutboxWriter
    where TContext : DbContext
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

public static class OutboxWriterServiceCollectionExtensions
{
    // Cada módulo que precisa gravar na outbox chama isto com o próprio DbContext — mantém
    // EfOutboxWriter<T> internal (só Common.Infrastructure precisa do tipo concreto).
    public static IServiceCollection AddOutboxWriter<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IOutboxWriter, EfOutboxWriter<TContext>>();
        return services;
    }
}
