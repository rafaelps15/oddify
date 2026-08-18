using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.Inbox;

public static class IntegrationEventHandlerRegistrationExtensions
{
    // Acha por reflexão cada IIntegrationEventHandler<T> concreto no assembly Presentation do
    // módulo e registra cada um puro — sem idempotência extra por handler. O projeto de referência
    // (Modular Monolith with DDD) não tem isso; a única proteção contra reprocessamento é
    // ProcessedOnUtc na própria linha da inbox.
    public static IServiceCollection AddIntegrationEventHandlers(this IServiceCollection services, Assembly presentationAssembly)
    {
        IEnumerable<Type> handlerTypes = presentationAssembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IIntegrationEventHandler)) && type != typeof(IIntegrationEventHandler));

        foreach (Type handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        return services;
    }
}
