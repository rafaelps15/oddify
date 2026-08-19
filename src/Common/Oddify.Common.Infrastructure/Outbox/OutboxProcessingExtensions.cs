using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox
{
    public static class OutboxProcessingExtensions
    {
        public static IServiceCollection AddOutboxProcessor(this IServiceCollection services, string schema)
        {
            services.AddSingleton(new OutboxModule(schema));

            services.AddSingleton<IConfigureOptions<QuartzOptions>>(sp =>
                new ConfigureOutboxProcessorJob(schema, sp.GetRequiredService<IOptions<OutboxProcessorOptions>>()));

            return services;
        }
    }
}
