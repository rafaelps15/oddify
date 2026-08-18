using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Oddify.Common.Application.Outbox;
using Quartz;

namespace Oddify.Common.Infrastructure.Outbox;

public static class OutboxProcessingExtensions
{
    // Chamado de dentro do composition root do próprio módulo (AddXModule) — não de Program.cs.
    // AddQuartz()/AddQuartzHostedService() já rodam uma vez em AddInfrastructure; isso só contribui
    // o IConfigureOptions<QuartzOptions> e o OutboxModule (pro cleanup) desse módulo específico.
    public static IServiceCollection AddOutboxProcessor(this IServiceCollection services, string schema)
    {
        services.AddSingleton(new OutboxModule(schema));

        services.AddSingleton<IConfigureOptions<QuartzOptions>>(sp =>
            new ConfigureOutboxProcessorJob(schema, sp.GetRequiredService<IOptions<OutboxProcessorOptions>>()));

        return services;
    }
}
