using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Infrastructure.Serialization;

namespace Oddify.Common.Infrastructure.Processing
{
    internal class EfCommandsScheduler<TContext> : ICommandsScheduler
        where TContext : DbContext
    {
        private readonly TContext _context;

        public EfCommandsScheduler(TContext context)
        {
            _context = context;
        }

        public Task EnqueueAsync(ICommand command)
        {
            var internalCommand = new InternalCommand(
                command.GetType().AssemblyQualifiedName!,
                JsonSerializer.Serialize(command, command.GetType(), EventSerializerOptions.Instance),
                DateTime.UtcNow);

            _context.Set<InternalCommand>().Add(internalCommand);

            return Task.CompletedTask;
        }
    }

    public static class CommandsSchedulerServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandsScheduler<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            services.AddScoped<ICommandsScheduler, EfCommandsScheduler<TContext>>();
            return services;
        }
    }
}
