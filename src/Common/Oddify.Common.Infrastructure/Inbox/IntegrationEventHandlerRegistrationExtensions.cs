using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.Inbox;

public static class IntegrationEventHandlerRegistrationExtensions
{
    // Espelho de AddDomainEventHandlers — acha por reflexão cada IIntegrationEventHandler<T>
    // concreto no assembly Presentation do módulo, registra cada um resolvendo pra uma versão
    // decorada com IdempotentIntegrationEventHandler<T>.
    public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, Assembly presentationAssembly, string schema)
    {
        IEnumerable<Type> handlerTypes = presentationAssembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IIntegrationEventHandler)) && type != typeof(IIntegrationEventHandler));

        foreach (Type handlerType in handlerTypes)
        {
            Type integrationEventType = handlerType.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .GetGenericArguments()[0];

            Type idempotentClosedType = typeof(IdempotentIntegrationEventHandler<>).MakeGenericType(integrationEventType);

            services.AddScoped(handlerType, sp =>
            {
                object innerHandler = ActivatorUtilities.CreateInstance(sp, handlerType);

                return ActivatorUtilities.CreateInstance(sp, idempotentClosedType, innerHandler, schema);
            });
        }

        return services;
    }
}
