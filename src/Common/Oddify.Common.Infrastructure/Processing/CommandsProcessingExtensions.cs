using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Processing
{
    public static class CommandsProcessingExtensions
    {
        // Chamado de dentro do composition root do próprio módulo (AddXModule) — não de Program.cs.
        // AddQuartz()/AddQuartzHostedService() já rodam uma vez em AddInfrastructure; isso só contribui
        // o IConfigureOptions<QuartzOptions> e o CommandsSchedulerModule (pro cleanup) desse módulo.
        public static IServiceCollection AddCommandsProcessor(this IServiceCollection services, string schema)
        {
            services.AddSingleton(new CommandsSchedulerModule(schema));

            services.AddSingleton<IConfigureOptions<QuartzOptions>>(sp =>
                new ConfigureInternalCommandProcessorJob(schema, sp.GetRequiredService<IOptions<OutboxProcessorOptions>>()));

            return services;
        }
    }
}
