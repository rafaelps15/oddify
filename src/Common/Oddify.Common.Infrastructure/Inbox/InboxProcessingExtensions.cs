using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Inbox
{
    public static class InboxProcessingExtensions
    {
        public static IServiceCollection AddInboxProcessor(this IServiceCollection services, string schema, Assembly presentationAssembly)
        {
            services.AddSingleton(new InboxModule(schema));

            services.AddSingleton<IConfigureOptions<QuartzOptions>>(sp =>
                new ConfigureInboxProcessorJob(schema, presentationAssembly, sp.GetRequiredService<IOptions<OutboxProcessorOptions>>()));

            return services;
        }
    }
}
