using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Processing
{
    public static class CommandsProcessingExtensions
    {
        public static IServiceCollection AddCommandsProcessor(this IServiceCollection services, string schema)
        {
            services.AddSingleton(new CommandsSchedulerModule(schema));

            services.AddSingleton<IConfigureOptions<QuartzOptions>>(sp =>
                new ConfigureInternalCommandProcessorJob(schema, sp.GetRequiredService<IOptions<OutboxProcessorOptions>>()));

            return services;
        }
    }
}
