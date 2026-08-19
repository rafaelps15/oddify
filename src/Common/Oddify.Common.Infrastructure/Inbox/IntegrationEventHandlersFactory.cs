using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.EventBus;

namespace Oddify.Common.Infrastructure.Inbox
{
    internal static class IntegrationEventHandlersFactory
    {
        private static readonly ConcurrentDictionary<string, Type[]> HandlerTypesByKey = new();

        public static IEnumerable<IIntegrationEventHandler> GetHandlers(Type integrationEventType, IServiceProvider serviceProvider, Assembly presentationAssembly)
        {
            Type[] handlerTypes = HandlerTypesByKey.GetOrAdd(
                $"{presentationAssembly.GetName().Name}:{integrationEventType.FullName}",
                _ => presentationAssembly.GetTypes()
                    .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEventType)))
                    .ToArray());

            return handlerTypes.Select(handlerType => (IIntegrationEventHandler)serviceProvider.GetRequiredService(handlerType));
        }
    }
}
