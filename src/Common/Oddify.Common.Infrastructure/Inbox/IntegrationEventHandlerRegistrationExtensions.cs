using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.Inbox
{
    public static class IntegrationEventHandlerRegistrationExtensions
    {
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
}
